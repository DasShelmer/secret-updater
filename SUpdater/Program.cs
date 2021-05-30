using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;

namespace SUpdater
{
    class Program
    {
        static string usbFolderName = "Secret";
        static string DBFilename = "Database.kdbx";
        static string CertFilename = "Certificate.keyx";
        static List<DriveInfo> usbBackupDrivers = new List<DriveInfo>();


        static Dictionary<string, HashFileInfo> DBs = new Dictionary<string, HashFileInfo>();
        static Dictionary<string, HashFileInfo> Certs = new Dictionary<string, HashFileInfo>();

        static HashFileInfo selectedDB = null;
        static HashFileInfo selectedCert = null;

        static void Main(string[] args)
        {
            try
            {
                Config.LoadConfig();
            }
            catch (FileLoadException e)
            {
                Console.WriteLine("{0} ({1})", e.Message, e.FileName);
                Console.ReadKey();
                return;
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("{0} ({1})", e.Message, e.FileName);
                Console.ReadKey();
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadKey();
                return;
            }
            usbBackupDrivers = GetUsbFlashes();
            LoadLocal();
            foreach (var drive in usbBackupDrivers)
            {
                LoadDrive(drive);
            }
            ShowDBs();
            SelectFiles();
            AppendFiles();

            Console.WriteLine("=======Готово!=======");

            Console.ReadKey();
        }
        public static string SHA256CheckSum(string filePath)
        {
            using (SHA256 SHA256 = SHA256Managed.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                    return Convert.ToBase64String(SHA256.ComputeHash(fileStream));
            }
        }
        public static List<DriveInfo> GetUsbFlashes()
        {
            var backupDrivers = new List<DriveInfo>();

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Removable)
                {
                    continue;
                }
                var secretFolder = Path.Combine(drive.RootDirectory.FullName, usbFolderName);
                if (Directory.Exists(secretFolder))
                {
                    backupDrivers.Add(drive);
                }
            }

            return backupDrivers;
        }
        public static bool Confirm(string title)
        {
            ConsoleKey response;
            do
            {
                Console.Write($"{ title } [y/n] ");
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }
            } while (
            response != ConsoleKey.Y && 
            response != ConsoleKey.N && 
            response != ConsoleKey.Enter && 
            response != ConsoleKey.Escape
            );

            return (response == ConsoleKey.Y || response == ConsoleKey.Enter);
        }
        static void LoadLocal()
        {
            var dbInfo = new HashFileInfo(Config.dbPath);

            if (dbInfo.Exists)
            {
                Console.WriteLine("Локальная БД существует:");
                Console.WriteLine("  SHA256: {0}", SHA256CheckSum(Config.dbPath));
                Console.WriteLine("  Дата создания:  {0}", File.GetCreationTime(Config.dbPath));
                Console.WriteLine("  Дата изменения: {0}", File.GetLastWriteTime(Config.dbPath));
                Console.WriteLine();

                DBs.TryAdd(SHA256CheckSum(Config.dbPath), dbInfo);
            }
            else
            {
                Console.WriteLine("Локальная БД не существует.");
                Console.WriteLine();
            }
        }

        static void LoadDrive(DriveInfo drive)
        {
            var secretPath = Path.Combine(drive.RootDirectory.FullName, usbFolderName);
            
            var files = Directory.GetFiles(secretPath);

            HashFileInfo cert = null;
            HashFileInfo db = null;
            foreach (var file in files)
            {
                if (file.EndsWith(CertFilename, StringComparison.CurrentCultureIgnoreCase))
                {
                    cert = new HashFileInfo(file);
                }
                else if (file.EndsWith(DBFilename, StringComparison.CurrentCultureIgnoreCase))
                {
                    db = new HashFileInfo(file);
                }
                if (cert != null && db != null)
                {
                    break;
                }
            }


            Console.WriteLine("Съёмный носитель {0}({1}):", drive.Name, drive.RootDirectory.FullName);
            if (db != null)
            {
                var hash = SHA256CheckSum(db.FullName);
                Console.WriteLine("Имеет БД:");
                Console.WriteLine("  SHA256: {0}", hash);
                Console.WriteLine("  Дата создания:  {0}", db.CreationTime);
                Console.WriteLine("  Дата изменения: {0}", db.LastWriteTime);

                DBs.TryAdd(hash, db);
            }
            else
            {
                Console.WriteLine("Не имеет БД.");
            }

            if (cert != null)
            {
                var hash = SHA256CheckSum(cert.FullName);
                Console.WriteLine("Имеет сертификат:");
                Console.WriteLine("  SHA256: {0}", hash);
                Console.WriteLine("  Дата создания: {0}", cert.CreationTime);

                Certs.TryAdd(hash, cert);
            }
            else
            {
                Console.WriteLine("Не имеет сертификата.");
            }
            Console.WriteLine();
        }

        static void ShowDBs()
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("Список всех БД:");
            for(int i = 0; i < DBs.Count; i++)
            {
                var dbpair = DBs.ElementAt(i);
                Console.WriteLine(" /Индекс: {0}", i);
                Console.WriteLine(" |SHA256: {0}", dbpair.Key);
                Console.WriteLine(" |Дата создания:  {0}", dbpair.Value.CreationTime);
                Console.WriteLine(" \\Дата изменения: {0}", dbpair.Value.LastWriteTime);
            }
            Console.WriteLine("Список всех Сертификатов:");
            for (int i = 0; i < Certs.Count; i++)
            {
                var certpair = Certs.ElementAt(i);
                Console.WriteLine(" /Индекс: {0}", i);
                Console.WriteLine(" |SHA256: {0}", certpair.Key);
                Console.WriteLine(" \\Дата создания:  {0}", certpair.Value.CreationTime);
            }
            Console.WriteLine("==========================================================");
        }

        static void SelectFiles()
        {
            if (DBs.Count == 1)
            {
                selectedDB = DBs.First().Value;
                Console.WriteLine("Т.к. БД одна, выбирается она.");
            }
            else
            {
                Console.Write("Выберите БД по индексу, которая будет скопирована на все места: ");
                while (selectedDB == null)
                {
                    string indexText = Console.ReadLine();
                    try
                    {
                        int index = Convert.ToInt32(indexText);
                        selectedDB = DBs.Values.ElementAt(index);
                    }
                    catch 
                    {
                        Console.Write("Некорректное значение, попробуйте снова: ");
                    }
                }
            }

            if (Certs.Count == 1)
            {
                selectedCert = Certs.First().Value;
                Console.WriteLine("Т.к. Cертификат один, выбирается он.");
            }
            else
            {
                Console.Write("Выберите Cертификат по индексу, который будет скопирован на все съёмные носители: ");
                while (selectedCert == null)
                {
                    string indexText = Console.ReadLine();
                    try
                    {
                        int index = Convert.ToInt32(indexText);
                        selectedCert = Certs.Values.ElementAt(index);
                    }
                    catch
                    {
                        Console.Write("Некорректное значение, попробуйте снова: ");
                    }
                }
            }
        }


        static void AppendFiles()
        {
            Console.WriteLine("=============================================================");
            var manualOrAuto = Confirm("Выберите режим замены файлов: ручной [y], автоматический [n]");

            var isAnyWasUpdated = false;

            var DB = File.ReadAllBytes(selectedDB.FullName);
            var Cert = File.ReadAllBytes(selectedCert.FullName);

            var needToUpdateLocal = !File.Exists(Config.dbPath) || selectedDB.Hash != SHA256CheckSum(Config.dbPath);
            if (needToUpdateLocal && (!manualOrAuto || Confirm("Произвести замену локальной БД?")))
            {
                var localDestination = new HashFileInfo(Config.dbPath).Directory;
                if (!localDestination.Exists) localDestination.Create();

                File.WriteAllBytes(Config.dbPath, DB);
                Console.WriteLine("  Замена локальной БД была произведена успешно.");
                isAnyWasUpdated = true;
            }

            foreach (var drive in usbBackupDrivers)
            {
                var dbPath = Path.Combine(drive.RootDirectory.FullName, usbFolderName, DBFilename);
                var dbNeedToUpdate = !File.Exists(dbPath) || selectedDB.Hash != SHA256CheckSum(dbPath);
                if (dbNeedToUpdate && (!manualOrAuto || Confirm(String.Format("Произвести замену БД на носителе {0}({1})?", drive.Name, drive.RootDirectory.FullName))))
                {
                    File.WriteAllBytes(dbPath, DB);

                    Console.WriteLine("  Замена БД на носителе {0}({1}) была произведена успешно.", drive.Name, drive.RootDirectory.FullName);
                    isAnyWasUpdated = true;
                }

                var certPath = Path.Combine(drive.RootDirectory.FullName, usbFolderName, CertFilename);
                var certNeedToUpdate = !File.Exists(certPath) || selectedCert.Hash != SHA256CheckSum(certPath);
                if (certNeedToUpdate && (!manualOrAuto || Confirm(String.Format("Произвести замену Сертификата на носителе {0}({1})?", drive.Name, drive.RootDirectory.FullName))))
                {
                    File.WriteAllBytes(Path.Combine(drive.RootDirectory.FullName, usbFolderName, CertFilename), Cert);

                    Console.WriteLine("  Замена Сертификата на носителе {0}({1}) была произведена успешно.", drive.Name, drive.RootDirectory.FullName);
                    isAnyWasUpdated = true;
                }
            }
            if (isAnyWasUpdated)
            {
                Console.WriteLine("Все требуемые замены были произведены успешно.");
            }
            else
            {
                Console.WriteLine("Замены не требуются.");
            }

        }
    }
}
