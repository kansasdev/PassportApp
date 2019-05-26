using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using java.lang;
using java.lang.reflect;

using javax.smartcardio;

using net.sf.scuba.smartcards;

using org.jmrtd;

using Zse6Devices.Winscard;

using ZSE6DevUnmanaged;

namespace PassportApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                /*
                opencard.core.terminal.CardTerminalRegistry ctr = opencard.core.terminal.CardTerminalRegistry.getRegistry();
                int ile = ctr.countCardTerminals();

                TerminalFactory tf = TerminalFactory.getDefault();
                java.security.Provider p = tf.getProvider();
                    CardTerminals cts = tf.terminals();
                    java.util.List lstCt = cts.list();
                */
                WinscardHelper wh = new WinscardHelper();
                List<string> czytniki = wh.PobierzDostepneCzytniki();

                string karta = string.Empty;
                string rdr = string.Empty;

                bool res = wh.PobierzPodlaczonaKarteICzytnik(ref karta, ref rdr);
                CardService                    

                    /*
                    CardService cs = CardService.getInstance(terminal);
                    TerminalCardService tcs = new TerminalCardService(ct);
                    PassportService ps = new PassportService(cs, 1500, 500, true, true);
                    ps.open();

                    //org.jmrtd.PassportService ps = new org.jmrtd.PassportService
                    */
                
            }
            catch(System.Exception ex)
            {
                MessageBox.Show("Błąd: " + ex.Message);
            }
        }
    }
}
