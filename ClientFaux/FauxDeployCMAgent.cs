﻿using Microsoft.ConfigurationManagement.Messaging.Framework;
using Microsoft.ConfigurationManagement.Messaging.Messages;
using Microsoft.ConfigurationManagement.Messaging.Sender.Http;
using System.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Diagnostics;
using System.Xml;
using static CMFaux.CMFauxStatusViewClasses;

namespace CMFaux
{
    class FauxDeployCMAgent
    {
        private static readonly HttpSender Sender = new HttpSender();
        
        public static SmsClientId RegisterClient(string CMServerName, string ClientName, string DomainName, string SiteCode, string outPutDirectory, string CertPath, string pass) {
            using (MessageCertificateX509Volatile certificate = new MessageCertificateX509Volatile(CertPath, pass))

            {
                X509Certificate2 thisCert = new X509Certificate2(CertPath, pass);


                Console.WriteLine(@"Using certificate for client authentication with thumbprint of '{0}'", certificate.Thumbprint);
                Console.WriteLine("Signature Algorithm: " + thisCert.SignatureAlgorithm.FriendlyName);

                if (thisCert.SignatureAlgorithm.FriendlyName == "sha256RSA")
                {
                    Console.WriteLine("Cert has a valid sha256RSA Signature Algorithm, proceeding");

                }
                else
                {
                     throw new Exception("Expected cert w/ a valid sha256RSA Signature Algorithm");
                }

                // Create a registration request
                ConfigMgrRegistrationRequest registrationRequest = new ConfigMgrRegistrationRequest();

                // Add our certificate for message signing
                registrationRequest.AddCertificateToMessage(certificate, CertificatePurposes.Signing | CertificatePurposes.Encryption);

                // Set the destination hostname
                registrationRequest.Settings.HostName = CMServerName;

                Console.WriteLine("Trying to reach: " + CMServerName);

                // Discover local properties for registration metadata
                registrationRequest.Discover();
                registrationRequest.AgentIdentity = "MyCustomClient";
                registrationRequest.ClientFqdn = ClientName + "." + DomainName;
                registrationRequest.NetBiosName = ClientName;
                //registrationRequest.HardwareId = Guid.NewGuid().ToString();
                Console.WriteLine("About to try to register " + registrationRequest.ClientFqdn);

                // Register client and wait for a confirmation with the SMSID

                //registrationRequest.Settings.Security.AuthenticationType = AuthenticationType.WindowsAuth;

                registrationRequest.Settings.Compression = MessageCompression.Zlib;
                registrationRequest.Settings.ReplyCompression = MessageCompression.Zlib;

                SmsClientId testclientId = new SmsClientId();
                try
                {
                    testclientId = registrationRequest.RegisterClient(Sender, TimeSpan.FromMinutes(5));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to enroll with an error");
                    Console.WriteLine(ex.Message);
                    System.Windows.MessageBox.Show("we failed with" + ex.Message);
                    throw;
                }
                SmsClientId clientId = testclientId;
                Console.WriteLine(@"Got SMSID from registration of: {0}", clientId);
                return clientId;
                }
            }

        public static void SendDiscovery(string CMServerName, string ClientName, string DomainName, string SiteCode, string outPutDirectory, string CertPath, string pass, SmsClientId clientId)
        {
            using (MessageCertificateX509Volatile certificate = new MessageCertificateX509Volatile(CertPath, pass))

            {
                X509Certificate2 thisCert = new X509Certificate2(CertPath, pass);

                Console.WriteLine(@"Got SMSID from registration of: {0}", clientId);

                // create base DDR Message
                ConfigMgrDataDiscoveryRecordMessage ddrMessage = new ConfigMgrDataDiscoveryRecordMessage();

                // Add necessary discovery data
                ddrMessage.SmsId = clientId;
                ddrMessage.ADSiteName = "Default-First-Site-Name"; //Changed from 'My-AD-SiteName
                ddrMessage.SiteCode = SiteCode;
                ddrMessage.DomainName = DomainName;
                ddrMessage.NetBiosName = ClientName;                

                Debug.WriteLine("ddrSettings clientID: " + clientId);
                Debug.WriteLine("ddrSettings SiteCode: " + ddrMessage.SiteCode);
                Debug.WriteLine("ddrSettings ADSiteNa: " + ddrMessage.ADSiteName);
                Debug.WriteLine("ddrSettings DomainNa: " + ddrMessage.DomainName);
                Debug.WriteLine("ddrSettings FakeName: " + ddrMessage.NetBiosName);
                Debug.WriteLine("Message MPHostName  : " + CMServerName);

                // Now create inventory records from the discovered data (optional)
                ddrMessage.Discover();                
                // Add our certificate for message signing
                ddrMessage.AddCertificateToMessage(certificate, CertificatePurposes.Signing);
                ddrMessage.AddCertificateToMessage(certificate, CertificatePurposes.Encryption);
                ddrMessage.Settings.HostName = CMServerName;
                //ddrMessage.Settings.Compression = MessageCompression.Zlib;
                //ddrMessage.Settings.ReplyCompression = MessageCompression.Zlib;
                Debug.WriteLine("Sending [" + ddrMessage.DdrInstances.Count + "] instances of Discovery data to CM");
                
                //see current value for the DDR message
                var OSSetting = ddrMessage.DdrInstances.OfType<InventoryInstance>().Where(m => m.Class == "CCM_DiscoveryData");
                
                ////retrieve actual setting
                //string OSCaption = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                //            select x.GetPropertyValue("Caption")).FirstOrDefault().ToString();

                //XmlDocument xmlDoc = new XmlDocument();               

                ////retrieve reported value
                //xmlDoc.LoadXml(ddrMessage.DdrInstances.OfType<InventoryInstance>().Where(m => m.Class == "CCM_DiscoveryData").FirstOrDefault().InstanceDataXml.ToString());

                ////Set OS to correct setting
                //xmlDoc.SelectSingleNode("/CCM_DiscoveryData/PlatformID").InnerText = "Microsoft Windows NT Server 10.0";
                
                ////Remove the instance
                //ddrMessage.DdrInstances.Remove(ddrMessage.DdrInstances.OfType<InventoryInstance>().Where(m => m.Class == "CCM_DiscoveryData").FirstOrDefault());

                //CMFauxStatusViewClassesFixedOSRecord FixedOSRecord = new CMFauxStatusViewClassesFixedOSRecord();
                //FixedOSRecord.PlatformId = OSCaption;
                //InventoryInstance instance = new InventoryInstance(FixedOSRecord);

                ////Add new instance
                //ddrMessage.DdrInstances.Add(instance);
                
                var updatedOSSetting = ddrMessage.DdrInstances.OfType<InventoryInstance>().Where(m => m.Class == "CCM_DiscoveryData");
                foreach (InventoryReportBodyElement Record in ddrMessage.DdrInstances)
                {

                    ddrMessage.DdrInstances.OfType<InventoryInstance>().Where(m => m.Class == "CCM_DiscoveryData");

                    //InventoryReportBodyElement OSClass = ddrMessage.DdrInstances.Where(m => m.Class == "CCM_DiscoveryData");                    
                    Debug.WriteLine(Record.ToString());
                }
                //ddrMessage.DdrInstances.Count
                // Now send the message to the MP (it's asynchronous so there won't be a reply)
                ddrMessage.SendMessage(Sender);            

                ConfigMgrHardwareInventoryMessage hinvMessage = new ConfigMgrHardwareInventoryMessage();
                hinvMessage.Settings.HostName = CMServerName;
                hinvMessage.SmsId = clientId;
                //hinvMessage.Settings.Security.EncryptMessage = true;
                hinvMessage.Discover();

                var Classes = CMFauxStatusViewClasses.GetWMIClasses();

                foreach (string Class in Classes)
                {

                    Console.WriteLine($"---Adding class : [{Class}]");
                    try { hinvMessage.AddInstancesToInventory(WmiClassToInventoryReportInstance.WmiClassToInventoryInstances(@"root\cimv2", Class)); }
                    catch { Console.WriteLine($"!!!Adding class : [{Class}] :( not found on this system"); }
                }

                var SMSClasses = new List<string> { "SMS_Processor", "CCM_System", "SMS_LogicalDisk" };
                foreach (string Class in SMSClasses)
                {

                    Console.WriteLine($"---Adding class : [{Class}]");
                    try { hinvMessage.AddInstancesToInventory(WmiClassToInventoryReportInstance.WmiClassToInventoryInstances(@"root\cimv2\sms", Class)); }
                    catch { Console.WriteLine($"!!!Adding class : [{Class}] :( not found on this system"); }
                }                

                hinvMessage.AddCertificateToMessage(certificate, CertificatePurposes.Signing | CertificatePurposes.Encryption);
                hinvMessage.Validate(Sender);
                hinvMessage.SendMessage(Sender);
            };
        }

        public static void GetPolicy(string CMServerName, string ClientName, string DomainName, string SiteCode, string outPutDirectory, string CertPath, string pass, SmsClientId clientId)
        {
            using (MessageCertificateX509Volatile certificate = new MessageCertificateX509Volatile(CertPath, pass))

            {
                X509Certificate2 thisCert = new X509Certificate2(CertPath, pass);
                ConfigMgrPolicyAssignmentRequest userPolicyMessage = new ConfigMgrPolicyAssignmentRequest();

                userPolicyMessage.AddCertificateToMessage(certificate, CertificatePurposes.Signing | CertificatePurposes.Encryption);
                userPolicyMessage.Settings.HostName = CMServerName;
                userPolicyMessage.ResourceType = PolicyAssignmentResourceType.User;
                userPolicyMessage.Settings.Security.AuthenticationScheme = AuthenticationScheme.Ntlm;
                userPolicyMessage.Settings.Security.AuthenticationType = AuthenticationType.WindowsAuth;
                userPolicyMessage.SmsId = clientId;
                userPolicyMessage.SiteCode = SiteCode;
                userPolicyMessage.Discover();
                userPolicyMessage.SendMessage(Sender);
                //userPolicyMessage.Settings.Security.EncryptMessage = encryption;
                //userPolicyMessage.Settings.ReplyCompression = (true == replyCompression) ? MessageCompression.Zlib : MessageCompression.None;
                //userPolicyMessage.Settings.Compression = (true == compression) ? MessageCompression.Zlib : MessageCompression.None;

                ConfigMgrPolicyAssignmentRequest machinePolicyMessage = new ConfigMgrPolicyAssignmentRequest();
                machinePolicyMessage.Settings.HostName = CMServerName;
                machinePolicyMessage.AddCertificateToMessage(certificate, CertificatePurposes.Signing | CertificatePurposes.Encryption);
                //machinePolicyMessage.Settings.Security.EncryptMessage = encryption;
                //machinePolicyMessage.Settings.Compression = (true == compression) ? MessageCompression.Zlib : MessageCompression.None;
                //machinePolicyMessage.Settings.ReplyCompression = (true == replyCompression) ? MessageCompression.Zlib : MessageCompression.None;
                machinePolicyMessage.ResourceType = PolicyAssignmentResourceType.Machine;
                machinePolicyMessage.SmsId = clientId;
                machinePolicyMessage.SiteCode = SiteCode;
                machinePolicyMessage.Discover();
                machinePolicyMessage.SendMessage(Sender);
            }

        }
    
    }   
}
