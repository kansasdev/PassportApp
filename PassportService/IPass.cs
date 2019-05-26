using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace PassportService
{
    [ServiceKnownType(typeof(ChipData))]
    [ServiceKnownType(typeof(PersonalDataVIZ))]
    [ServiceKnownType(typeof(KeyData))]
    public interface IPassCallback
    {
        [OperationContract]
        void SessionOpened();

        [OperationContract]
        void SessionFinished(string messages);

        [OperationContract]
        void ErrorRaised(string err);

        [OperationContract]
        void MrzFromChipTaken(string mrz);

        [OperationContract]
        void ImageFromChipTaken(byte[] img);

        [OperationContract]
        void DocChecksPerformed();

        [OperationContract]
        void ResultChip(ChipData cd);
        [OperationContract]
        void CSCADownloaded(string messages);

        [OperationContract]
        void CSCAUnzipped(string message);

        [OperationContract]
        void CSCAParsed(string message);
       
    }


    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IPass" in both code and config file together.
    [ServiceContract(CallbackContract = typeof(IPassCallback))]
    [ServiceKnownType(typeof(ChipData))]
    [ServiceKnownType(typeof(PersonalDataVIZ))]
    [ServiceKnownType(typeof(KeyData))]
    public interface IPass
    {
        [OperationContract(IsOneWay =false)]
        PersonalDataVIZ ExtractMrzData(byte[] image);

        [OperationContract(IsOneWay = false)]
        void ReadChip(KeyData kd);

        [OperationContract]
        bool CheckJavaPrerequisite(ref string message);

        [OperationContract]
        bool CheckPythonPrerequisite(ref string message);

        [OperationContract]
        bool InvokeMasterlistupdate(ref string message);
    }

    [DataContract]
    public class KeyData
    {
        [DataMember]
        public string DOB { get; set; }

        [DataMember]
        public string ValidityDate { get; set; }

        [DataMember]
        public string DocNumber { get; set; }
    }

    [DataContract]
    public class PersonalDataVIZ
    {
        [DataMember]
        public string IssuuingCountry { get; set; }
        /// <summary>
        /// yyMMdd
        /// </summary>
        [DataMember]
        public string DateOfBirth { get; set; }
        [DataMember]
        public bool DOBChecksum { get; set; }

        /// <summary>
        /// yyMMdd
        /// </summary>
        [DataMember]
        public string ExpirationDate { get; set; }
        [DataMember]
        public bool ExpirationChecksum { get; set; }

        [DataMember]
        public string DocumentNumber { get; set; }

        [DataMember]
        public bool DocNumberChecksum { get; set; }

        [DataMember]
        public string Sex { get; set; }
        [DataMember]
        public string Citizenship { get; set; }
        [DataMember]
        public string Firstname { get; set; }
        [DataMember]
        public string Lastname { get; set; }
        [DataMember]
        public string Errors { get; set; }
    }

    [DataContract]
    public class ChipData
    {
        [DataMember]
        public string MRZ { get; set; }
        [DataMember]
        public string LastName { get; set; }
        
        [DataMember]
        public string FirstName
        {
            get;set;    
        }

        [DataMember]
        public string DOB { get; set; }
        [DataMember]
        public string ExpirationDate { get; set; }
        [DataMember]
        public string IssuingCountry { get; set; }
        [DataMember]
        public string Citizenship { get; set; }
        [DataMember]
        public string DocumentNumber { get; set; }
        [DataMember]
        public string DocumentType { get; set; }

        [DataMember]
        public List<string> MessageLOG { get; set; }
        [DataMember]
        public bool DG1Integrity { get; set; }
        [DataMember]
        public bool DG2Integrity { get; set; }
        [DataMember]
        public bool? DG14Integrity { get; set; }
        [DataMember]
        public bool SODIntegrity { get; set; }

        [DataMember]
        public bool IsPACEReadingProc { get; set; }
        [DataMember]
        public byte[] ImagePhoto { get; set; }

        [DataMember]
        public bool IsJPEG2000 { get; set; }

        [DataMember]
        public bool? CAStatus { get; set; }
        [DataMember]
        public bool? IsDSTrusted { get; set; }



    }

}
