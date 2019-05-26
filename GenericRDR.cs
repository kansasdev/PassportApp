using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Zse6Devices.Winscard;

namespace PassportApp
{
    public class GenericRDR
    {


        private ZSE6DevUnmanaged.ManagedWinscard _monitorRFID;
        private ZSE6DevUnmanaged.ManagedWinscard _apduRDR;

        private bool _czyZainicjalizowany = false;
        private string _pasek;

        delegate void ActionVicomp(bool stan, int status);
        private ActionVicomp _actionVicomp;
        private Thread TNasluchiwanie;
        private DateTime CzasOdczytu;

        int iloscWyslanychPowiadomien;

        string _czytnik = string.Empty;

        private enum StanPoszukiwaniaKrty
        {
            BladInicjalizacjiKontekstu = 0,
            BladPobieraniaListyCzytnikow = 1,
            BladLokalizowaniaKart = 2,
            BladPobieraniaStanuKarty = 3,
            BladZwalnianiaKontekstu = 4,
            BrakCzytnikow = 5,
            PetlaOczekiwania = 6,
            DokumentPrzyCzytniku = 7,
            BladLaczeniaZCzytnikiem = 8,
            BladRozlaczaniaZCzytnikiem = 9,
            ZakonczonoMonitorowanie = 10
        };


        public bool InicjalizujCzytnikDokumentow(ref string komunikatZwrotny, string czytnik)
        {
            if (_monitorRFID == null)
            {
                TNasluchiwanie = new Thread(() =>
                {
                    _actionVicomp = new ActionVicomp(ZmianaStanuKarty);
                    _monitorRFID = new ZSE6DevUnmanaged.ManagedWinscard();

                    string komunikat = string.Empty;
                    CzasOdczytu = DateTime.Now;
                    _monitorRFID.MonitorujCzytnikRFID(_actionVicomp, "", ref komunikat);
                });
                TNasluchiwanie.Start();
            }

            return true;

        }

                      

        private void ZmianaStanuKarty(bool stan, int status)
        {


            string komunikat = string.Empty;
            switch (status)
            {
                case (int)StanPoszukiwaniaKrty.BladInicjalizacjiKontekstu:
                    komunikat = "Błąd inicjalizacji kontekstu";
                    OnPrzyszedlLogZCzytnikaPaszportow(komunikat);
                    break;

                case (int)StanPoszukiwaniaKrty.BladLokalizowaniaKart:
                    komunikat = "Błąd lokalizowania chipu RFID";
                    OnPrzyszedlLogZCzytnikaPaszportow(komunikat);
                    break;

                case (int)StanPoszukiwaniaKrty.BladPobieraniaListyCzytnikow:
                    komunikat = "Błąd pobierania listy czytników";
                    OnPrzyszedlLogZCzytnikaPaszportow(komunikat);
                    break;

                case (int)StanPoszukiwaniaKrty.BladPobieraniaStanuKarty:
                    komunikat = "Błąd pobierania stanu karty";
                    OnPrzyszedlLogZCzytnikaPaszportow(komunikat);
                    break;

                case (int)StanPoszukiwaniaKrty.BladZwalnianiaKontekstu:
                    komunikat = "Błąd zwalniania kontekstu";
                    OnPrzyszedlLogZCzytnikaPaszportow(komunikat);
                    break;

                case (int)StanPoszukiwaniaKrty.BrakCzytnikow:
                    komunikat = "Brak czytników RFID w systemie";
                    OnPrzyszedlLogZCzytnikaPaszportow(komunikat);
                    break;

                case (int)StanPoszukiwaniaKrty.PetlaOczekiwania:
                    if (iloscWyslanychPowiadomien < 5)
                    {
                        komunikat = "Czekam na przyłożenie dokumentu.." + Environment.NewLine;

                        OnPrzyszedlLogZCzytnikaPaszportow(komunikat);
                        // oczekiwanie 5 sekund
                        var t = Task.Delay(1000);
                        t.GetAwaiter().GetResult();
                    }
                    else
                    {
                        OnPrzyszedlLogZCzytnikaPaszportow("Zakończono monitorowanie");
                        OnZakonczonoOdczytDokumentu();
                        OnZdjetoDokument();
                        _monitorRFID = null;
                        TNasluchiwanie.Abort();
                        TNasluchiwanie.Join();
                    }
                    iloscWyslanychPowiadomien++;

                    break;

                case (int)StanPoszukiwaniaKrty.DokumentPrzyCzytniku:


                    CzasOdczytu = DateTime.Now;
                    OnPrzyszedlLogZCzytnikaPaszportow("Rozpoczynam odczyt dokumentu..");

                    OnRozpoczetoOdczytDokumentu();

                    if (!string.IsNullOrEmpty(_pasek))
                    {
                        //MUSISZ TAK ZROBIC BO MONITOR JEST NA OSOBNYM WĄTKU
                        if (_apduRDR == null)
                        {
                            _apduRDR = new ZSE6DevUnmanaged.ManagedWinscard();
                        }
                        PaszportBiometryczny pb = new PaszportBiometryczny(_pasek, _apduRDR, "");
                        pb.ProcentOfRead += pb_ProcentOfRead;
                        string komunikatPaszport = string.Empty;
                        bool wynik = pb.OdczytajPaszport(ref komunikatPaszport);
                        if (wynik)
                        {
                            bool czyPrzeprowadzonoPoprawniePA = true;
                            if (pb.DSCert != null)
                            {
                                //List<X509Certificate2> lstCsca = _kcp.WyciagnijOdpowiedniCertyfikat(pb.DSCert);
                                List<X509Certificate2> lstCsca = new List<X509Certificate2>();

                                pb.WeryfikujPaszport(lstCsca, ref komunikatPaszport);

                                if (lstCsca != null)
                                {
                                    if (pb.CzyDSZaufany)
                                    {
                                        OnPrzyszedlLogZCzytnikaPaszportow("POPRAWNY PODPIS WYSTAWCY");
                                    }
                                    else
                                    {
                                        OnPrzyszedlLogZCzytnikaPaszportow("NIEPOPRAWNY PODPIS WYSTAWCY");
                                    }

                                }


                            }
                            else
                            {
                                OnPrzyszedlLogZCzytnikaPaszportow("NIE MOGĘ ZWERYFIKOWAC PASZPORTU - BRAK CERTYFIKATU WYSTAWCY");

                            }
                               
                            

                            if (pb.CzyJestBAC)
                            {
                                
                            }

                            if (pb.DG2 != null)
                            {
                                
                                string kom = string.Empty;
                                
                                Bitmap bmp = ParsujObrazekZPaszportu(pb.DG2, ref kom);
                                
                                if (pb.CzyDG2TakieSameZWyliczonym)
                                {
                                    
                                }
                                else
                                {
                                    
                                }
                                


                            }

                            if (pb.DG1 != null)
                            {
                                if (pb.DG1[0] == 0x61)
                                {
                                    int rozmiar = Convert.ToInt32(BitConverter.ToString(pb.DG1, 1, 1), 16);
                                    string ascii = ASCIIEncoding.Default.GetString(pb.DG1, 3, rozmiar - 1).Substring(2);
                                    StringBuilder sb = new StringBuilder();
                                    string pasekRfid = ascii;
                                    if (ascii.Length == 88)
                                    {
                                        string linia1 = pasekRfid.Substring(0, 44);
                                        string linia2 = pasekRfid.Remove(0, 44);

                                        sb.Append(linia1);
                                        sb.Append(Environment.NewLine);
                                        sb.Append(linia2);
                                    }

                                    if (ascii.Length == 90)
                                    {
                                        string linia1 = pasekRfid.Substring(0, 30);
                                        string linia2 = pasekRfid.Remove(0, 30).Remove(30, 30);
                                        string linia3 = pasekRfid.Remove(0, 60);
                                        sb.Append(linia1);
                                        sb.Append(Environment.NewLine);
                                        sb.Append(linia2);
                                        sb.Append(Environment.NewLine);
                                        sb.Append(linia3);
                                    }

                                   


                                }
                                else
                                {
                                    OnPrzyszedlErrorZCzytnikaPaszportow("Błąd parsowania paska mrz z grupy DG1");
                                }

                              
                                if (pb.CzyDG1TakieSameZWyliczonym)
                                {
                                    
                                }
                                else
                                {
                                    
                                }
                                
                            }

                            if (pb.DG3 != null)
                            {
                                
                                if (pb.CzyDG3TakieSameZWyliczonym)
                                {
                                    
                                }
                                else
                                {
                                   
                                }
                                
                            }

                            if (pb.DG14 != null)
                            {
                                if (pb.CzyDG14TakieSameZWyliczonym)
                                {
                                   
                                }
                                else
                                {
                                    
                                }
                                
                            }

                            if (pb.DG15 != null)
                            {
                               
                                if (pb.CzyDG15TakieSameZWyliczonym)
                                {
                                    
                                }
                                else
                                {
                                    
                                }
                                
                            }

                            if (pb.EFSOD != null)
                            {
                                
                                if (pb.CzyEFSODHashPoprawne)
                                {
                                    
                                }
                                else
                                {
                                    
                                }
                               
                            }

                            if (pb.CzyDSZaufany)
                            {
                                
                                if (czyPrzeprowadzonoPoprawniePA)
                                {
                                   
                                }
                                else
                                {
                                    
                                }
                                
                            }
                            else
                            {
                                
                                if (czyPrzeprowadzonoPoprawniePA)
                                {
                                    
                                }
                                else
                                {
                                    
                                }
                                
                            }

                        }
                        else
                        {
                            OnPrzyszedlErrorZCzytnikaPaszportow(komunikatPaszport);
                        }
                    }

                    _apduRDR.ZakonczOdczytRFID(ref komunikat);
                    OnPrzyszedlLogZCzytnikaPaszportow(komunikat);
                    OnZdjetoDokument();
                    OnZakonczonoOdczytDokumentu();

                    break;
                case (int)StanPoszukiwaniaKrty.ZakonczonoMonitorowanie:

                    OnPrzyszedlLogZCzytnikaPaszportow("Zakończono monitorowanie");
                    OnZakonczonoOdczytDokumentu();
                    OnZdjetoDokument();
                    _monitorRFID = null;
                    TNasluchiwanie.Abort();
                    TNasluchiwanie.Join();



                    break;
            }

        }

        void pb_ProcentOfRead(int obj)
        {
            OnPrzyszedlPostepOdczytuCzytnikaCalostronicowego(obj);
        }

        public Bitmap ParsujObrazekZPaszportu(byte[] dane, ref string komunikatZwrotny)
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                string DG2 = BitConverter.ToString(dane, 0, 1);
                int indx = BitConverter.ToString(dane).IndexOf("5F-2E");
                int imgIxA = BitConverter.ToString(dane).IndexOf("FF-D8");
                int imgIxB = BitConverter.ToString(dane).IndexOf("0C-6A");
                int DG2Size = Convert.ToInt32(BitConverter.ToString(dane, (indx / 3) + 3, 2).Replace("-", ""), 16); ;
                int sizeImg = 0;
                int indxImg = 0;

                if ((imgIxA < imgIxB && imgIxA > 0) || imgIxB == -1)
                {
                    indxImg = (imgIxA / 3);
                    sizeImg = DG2Size - ((imgIxA / 3) - ((indx / 3) + 3));
                    ms = new MemoryStream(dane, indxImg, sizeImg);
                    Image img = Image.FromStream(ms);

                    return new Bitmap(img);

                }
                else
                {
                    indxImg = (imgIxB / 3) - 3;
                    sizeImg = dane.Length - indxImg;
                    ms = new MemoryStream(dane, indxImg, sizeImg);

                    //WsqNistWrapper.WsqNistWrapperClass wc = new WsqNistWrapper.WsqNistWrapperClass();
                    //string sciezka = Directory.GetCurrentDirectory();
                    byte[] daneBMP = new byte[] { };
                    //string zwrotka = wc.DekompresujJP2000(ms.ToArray(), ref daneBMP);
                    ms.Close();
                    //if (zwrotka.StartsWith("OK"))
                    {
                        ms = new MemoryStream(daneBMP);
                        Image img = Image.FromStream(ms);
                        Bitmap bmp = new Bitmap(img);
                        ms.Close();
                        return bmp;
                    }
                    //else
                    //{
                    //    return null;
                    //}
                }

            }
            catch (Exception ex)
            {
                komunikatZwrotny = "Błąd parsowania pliku: " + ex.Message;
                return null;
            }

        }

        public bool ZamknijCzytnikDokumentow(ref string komunikatZwrotny)
        {
            try
            {
                
                _czyZainicjalizowany = false;

                if (_monitorRFID != null)
                {
                    _monitorRFID.ZakonczMonitorowanieCzytnikaRFID();
                }



                return true;
            }
            catch (Exception ex)
            {
                komunikatZwrotny = "Nie udało się zamknąć czytnika paszportów";
                return false;
            }
        }

        public event EventHandler<string> PrzyszlyDaneMRZCzytnikaPaszportow;
        public event EventHandler<string> PrzyszedlErrorZCzytnikaPaszportow;
        public event EventHandler<string> PrzyszedlLogZCzytnikaPaszportow;
        public event EventHandler<int> PrzyszedlPostepOdczytuCzytnikaCalostronicowego;
        
        public event EventHandler RozpoczetoOdczytDokumentu;
        public event EventHandler ZakonczonoOdczytDokumentu;
        public event EventHandler ZdjetoDokument;

        public event Func<byte[], string, byte[]> WyslanoZadaniePodpisu;
        private void OnWyslanoZadaniePodpisu(string numerSeryjny, byte[] hashDoPodpisu)
        {
            if (WyslanoZadaniePodpisu != null)
            {
                WyslanoZadaniePodpisu(hashDoPodpisu, numerSeryjny);
            }
        }


        private void OnPrzyszlyDaneMRZCzytnikaPaszportow(string pasek)
        {
            if (PrzyszlyDaneMRZCzytnikaPaszportow != null)
            {
                PrzyszlyDaneMRZCzytnikaPaszportow(this, pasek);


            }
        }

        private void OnPrzyszedlLogZCzytnikaPaszportow(string logKomunikat)
        {
            if (PrzyszedlLogZCzytnikaPaszportow != null)
            {
                PrzyszedlLogZCzytnikaPaszportow(this,logKomunikat);
            }
        }

        private void OnPrzyszedlErrorZCzytnikaPaszportow(string blad)
        {
            if (PrzyszedlErrorZCzytnikaPaszportow != null)
            {
                PrzyszedlErrorZCzytnikaPaszportow(this, blad);
            }
        }



        private void OnPrzyszedlPostepOdczytuCzytnikaCalostronicowego(int postep)
        {
            if (PrzyszedlPostepOdczytuCzytnikaCalostronicowego != null)
            {
                PrzyszedlPostepOdczytuCzytnikaCalostronicowego(this, postep);
            }
        }

        private void OnZdjetoDokument()
        {
            if (ZdjetoDokument != null)
            {
                ZdjetoDokument(this, EventArgs.Empty);
            }
        }

        private void OnRozpoczetoOdczytDokumentu()
        {
            if (RozpoczetoOdczytDokumentu != null)
            {
                RozpoczetoOdczytDokumentu(this, EventArgs.Empty);
            }
        }

        private void OnZakonczonoOdczytDokumentu()
        {
            if (ZakonczonoOdczytDokumentu != null)
            {
                ZakonczonoOdczytDokumentu(this, EventArgs.Empty);
            }
        }
                       
        public void OdczytajWTrybiePACE()
        {
            throw new NotImplementedException("NIE ZAIMPLEMENTOWANO");
        }
    }
}
