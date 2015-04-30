using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SolFS;
using System.IO;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace EldosFileLib
{
    public class EldosFileSystem : IDisposable
    {
        private string _RegKey = "C05B5ADAD4C53C9A3F988DBA5FBC119ED39B6FF6F71BC83C88FDAACFDF49665B37A61F3CF2B8C4B7272BDE8639E0F92981A03614C4DBD8CDFABF1C71FE649FF58878C3E7BF6E00DBE4C3752DFF23B299B7D8A5172FA71D4909CF618E48BD6A8F6C7CC8";
        private SolFS.SolFSStorage Storage = null;
        private Hashtable StorageStreams = new Hashtable(1);
        public string FileStorageLocation { get; set; }
        public NTree<VirtualDirectory> TreeFolders { get; set; }
        public ConcurrentBag<string> Logs { get; set; }

        /// <summary>
        /// Initializes a new instance of the EldosFileSystem class.
        /// </summary>
        public EldosFileSystem()
            : this(@"C:\eldosStorage\desktop.st")
        {
            
        }

        public EldosFileSystem(string storageLocation)
        {
            FileStorageLocation = storageLocation;
            SetRegistration();
            CreateFileStorage(FileStorageLocation);
            Logs = new ConcurrentBag<string>();

        }



        public void CreateFileStorage(string storagePath)
        {
            if (!File.Exists(storagePath))
                Storage = new SolFS.SolFSStorage(storagePath, true, 512, false, false, '\\', "SolFS Explorer sample storage",
                    new SolFSCreateFileEvent(OnCreateFile),
                    new SolFSOpenFileEvent(OnOpenFile),
                    new SolFSCloseFileEvent(OnCloseFile),
                    new SolFSFlushFileEvent(OnFlushFile),
                    new SolFSDeleteFileEvent(OnDeleteFile),
                    new SolFSGetFileSizeEvent(OnGetFileSize),
                    new SolFSSetFileSizeEvent(OnSetFileSize),
                    new SolFSSeekFileEvent(OnSeekFile),
                    new SolFSReadFileEvent(OnReadFile),
                    new SolFSWriteFileEvent(OnWriteFile));
            else
            {
                Storage = new SolFS.SolFSStorage();

                Storage.FileName = storagePath;
                Storage.UseTransactions = true;
                Storage.PathSeparator = '\\';
                Storage.UseTransactions = false;
                Storage.UseAccessTime = false;

                Storage.OnHashCalculate = new SolFS.SolFSCalculateHashEvent(Sample_CalculateHash);
                Storage.OnHashValidate = new SolFS.SolFSValidateHashEvent(Sample_ValidateHash);
                Storage.OnDataEncrypt = new SolFS.SolFSCryptDataEvent(Sample_EncryptData);
                Storage.OnDataDecrypt = new SolFS.SolFSCryptDataEvent(Sample_DecryptData);

                Storage.Open(StorageOpenMode.somOpenExisting);


            }
                //Storage = new SolFS.SolFSStorage(storagePath,
                //              false,
                //              false,
                //              '\\',
                //              false,
                //              new SolFSCreateFileEvent(OnCreateFile),
                //              new SolFSOpenFileEvent(OnOpenFile),
                //              new SolFSCloseFileEvent(OnCloseFile),
                //              new SolFSFlushFileEvent(OnFlushFile),
                //              new SolFSDeleteFileEvent(OnDeleteFile),
                //              new SolFSGetFileSizeEvent(OnGetFileSize),
                //              new SolFSSetFileSizeEvent(OnSetFileSize),
                //              new SolFSSeekFileEvent(OnSeekFile),
                //              new SolFSReadFileEvent(OnReadFile),
                //              new SolFSWriteFileEvent(OnWriteFile));

        }

        public void OnCreateFile(SolFSStorage Sender, String FileName, ref UInt32 File, bool Overwrite, bool IsJournalFile, ref int Result)
        {
            FileStream stream = null;
            try
            {
                
                if (Overwrite)
                {
                    stream = new FileStream(FileName, FileMode.Create);
                    Result = 0;
                }
                else
                {
                    try
                    {
                        stream = new FileStream(FileName, FileMode.CreateNew);
                        Result = 0;
                    }
                    catch (IOException)
                    {
                        Result = 183; //ERROR_ALREADY_EXISTS
                    }
                }
                if (Result == 0)
                {
                    int hashcode = stream.GetHashCode();
                    if (hashcode < 0)
                        hashcode += 0x7fffffff;
                    File = (uint)hashcode;
                    StorageStreams.Add(File, stream);
                }
                
            }
            catch (Exception)
            {
                Result = -1;
                File = 0xffffffff;
            }
            finally
            {
                stream.Close();
            }
        }

        public void OnOpenFile(SolFSStorage Sender, String FileName, ref UInt32 File, ref bool ReadOnly, bool IsJournalFile, ref int Result)
        {
            FileStream stream = null;
            try
            {
                
                   try
                    {
                        stream = new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite);
                        ReadOnly = false;
                        Result = 0;
                    }
                    catch (FileNotFoundException)
                    {
                        Result = 2; //ERROR_FILE_NOT_FOUND
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Result = 5;
                    }

                    if (Result == 5)
                    {
                        stream = new FileStream(FileName, FileMode.Open, FileAccess.Read);
                        ReadOnly = true;
                        Result = 0;
                    }

                    if (Result == 0)
                    {
                        int hashcode = stream.GetHashCode();
                        if (hashcode < 0)
                            hashcode += 0x7fffffff;
                        File = (uint)hashcode;
                        StorageStreams.Add(File, stream);
                    }
                
            }
            catch (Exception)
            {
                Result = -1;
                File = 0xffffffff;
            }
            finally
            {
                stream.Close();
            }
        }

        public void OnCloseFile(SolFSStorage Sender, ref UInt32 File, ref int Result)
        {
            using (FileStream stream = (FileStream)FindStream(File))
            {
                StorageStreams.Remove(File);
                try
                {
                    stream.Close();
                    Result = 0;
                }
                catch (Exception)
                {
                    Result = Result = -1;
                }

                File = 0xffffffff;
            }
        }

        public void OnFlushFile(SolFSStorage Sender, UInt32 File, ref int Result)
        {
            try
            {
                using (FileStream stream = (FileStream)FindStream(File))
                {
                    stream.Flush();
                    Result = 0;
                }
            }
            catch (Exception)
            {
                Result = -1;
            }
        }

        public void OnDeleteFile(SolFSStorage Sender, String FileName, ref int Result)
        {
            try
            {
                File.Delete(FileName);
            }
            catch (Exception)
            {
                Result = -1;
            }
        }

        public void OnGetFileSize(SolFSStorage Sender, UInt32 File, ref UInt64 Size, ref int Result)
        {
            using (FileStream stream = (FileStream)FindStream(File))
            {
                Size = (ulong)stream.Length;
                Result = 0;
            }
        }

        public void OnSetFileSize(SolFSStorage Sender, UInt32 File, UInt64 NewSize, ref int Result)
        {
            try
            {
                using (FileStream stream = (FileStream)FindStream(File))
                {
                    stream.SetLength((long)NewSize);
                    Result = 0;
                }
            }
            catch (Exception)
            {
                Result = -1;
            }
        }

        public void OnSeekFile(SolFSStorage Sender, UInt32 File, Int64 Offset, UInt16 Origin, ref int Result)
        {

            SeekOrigin origin;
            switch (Origin)
            {
                case 0:
                    origin = SeekOrigin.Begin;
                    break;
                case 1:
                    origin = SeekOrigin.Current;
                    break;
                case 2:
                    origin = SeekOrigin.End;
                    break;
                default:
                    origin = SeekOrigin.Begin;
                    break;
            }
            
            try
            {
                using (FileStream stream = (FileStream)FindStream(File))
                {
                    stream.Seek(Offset, origin);
                    Result = 0;
                }
            }
            catch (Exception)
            {
                Result = -1;
            }
        }

        public void OnReadFile(SolFSStorage Sender, UInt32 File, byte[] buffer, UInt32 Count, ref int Result)
        {
            try
            {
                using (FileStream stream = (FileStream)FindStream(File))
                {
                    int read = stream.Read(buffer, 0, (int)Count);
                    if (read == Count)
                        Result = 0;
                    else
                        Result = -1;
                }
            }
            catch (Exception)
            {
                Result = Result = -1;
            }
        }

        public void OnWriteFile(SolFSStorage Sender, UInt32 File, byte[] buffer, UInt32 Count, ref int Result)
        {
            try
            {
                using(FileStream stream = (FileStream)FindStream(File))
                {
                    stream.Write(buffer, 0, (int)Count);
                    Result = 0;
                }
               
            }
            catch (Exception)
            {
                Result = Result = -1;
            }
        }

        public void Dispose()
        {
            if (Storage != null)
            {
                Storage.Dispose();
                Storage = null;
            }
        }

        protected Stream FindStream(UInt32 Handle)
        {
            return (Stream)StorageStreams[Handle];
        }

        private void AddSubFolders(string BasePath, NTree<VirtualDirectory> BaseNode)
        {
            NTree<VirtualDirectory> CurrentNode = null;

            SolFS.StorageSearch SearchStruct = new SolFS.StorageSearch();

            String mask = "";
            if (BasePath.Equals("\\"))
                mask = "\\*";
            else
                mask = BasePath + "\\*";

            bool b = Storage.FindFirst(mask, SolFSFileAttribute.attrAnyFile, ref SearchStruct);
            if (b == true)
            {
                while (b == true)
                {
                    if ((SearchStruct.Attributes & SolFSFileAttribute.attrDirectory) == SolFSFileAttribute.attrDirectory)
                    {
                        BaseNode.AddNode(new VirtualDirectory() { Name = SearchStruct.FileName, Type = NodeType.Directory }); ;
                        CurrentNode = BaseNode;
                        AddSubFolders(SearchStruct.FullName, CurrentNode);
                    }
                    b = Storage.FindNext(ref SearchStruct);
                }
                Storage.FindClose(ref SearchStruct);
            }
        }


        private void RebuildFolders()
        {
            var RootNode = new NTree<VirtualDirectory>(new VirtualDirectory { Name = "\\", Type = NodeType.Directory });
            AddSubFolders("\\", RootNode);

            TreeFolders = RootNode;

        }

        private bool FileExists(String FileName)
        {
            SolFS.StorageSearch SearchStruct = new SolFS.StorageSearch();
            bool b = Storage.FindFirst(FileName, SolFSFileAttribute.attrAnyFile, ref SearchStruct);
            if (b)
                Storage.FindClose(ref SearchStruct);
            return b;
        }

        private NTree<VirtualDirectory> FindNode(NTree<VirtualDirectory> ParentNode, string Text)
        {
            //TreeNode Result = null;
            //TreeNodeCollection nodes;
            //if (ParentNode == null)
            //	nodes = treeFolders.Nodes;
            //else
            //	nodes = ParentNode.Nodes;

            //string lText = Text.ToLower();

            //for (int i = 0; i < nodes.Count - 1; i++)
            //{
            //	if (nodes[i].Text.ToLower().Equals(lText))
            //	{
            //		Result = nodes[i];
            //		break;
            //	}
            //}

            //return Result;
            throw new NotImplementedException();
        }

        private void SetRegistration()
        {
            try
            {
                SolFSStorage.SetRegistrationKey(_RegKey);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void AddFiles(string path, IEnumerable<string> fileNames)
        {

            Parallel.ForEach(fileNames, currentFile =>
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                using (var fileStream = new FileStream(string.Format(@"{0}\{1}", path, currentFile), FileMode.Open, FileAccess.Read))
                {
                    AddFile(null, currentFile, fileStream);
                }
                stopwatch.Stop();
                Logs.Add(string.Format("File Name: {0}, current seconds:{1}, current ms:{2}  ProcessTime: {3}", currentFile, DateTime.Now.ToString("ss"), DateTime.Now.ToString("ffff"), (stopwatch.ElapsedMilliseconds / 1000) == 0 ? string.Format("0.{0}s", stopwatch.ElapsedMilliseconds) : string.Format("{0}s", stopwatch.ElapsedMilliseconds / 1000)));

            });
        }



        public string UniqueFileName(string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var random = new Random();
                var result = new string(
                    Enumerable.Repeat(chars, 8)
                              .Select(s => s[random.Next(s.Length)])
                              .ToArray());

                var fileParts = fileName.Split('.');

                if (fileParts != null && fileParts.Length == 2)
                    fileName = string.Format("{0}{1}.{2}", fileParts[0], result, fileParts[1]);

            }
            return fileName;
        }


        public void AddFile(string path, string fileName, Stream fileStream)
        {
            if (!string.IsNullOrWhiteSpace(fileName) && fileStream != null)
            {
                
                //string fullName = GetFullFileName(path, fileName);


                string fullName = UniqueFileName(GetFullFileName(path, fileName));
                
                
                var config = new SolFSStreamModeConfig(SolFSStreamMode.Write);

                try
                {
                    using (SolFS.SolFSStream solFSStream = new SolFS.SolFSStream(storage: Storage,
                                                               fileName: fullName,
                                                               createNew: config.CreateNew,
                                                               readEnabled: config.ReadEnabled,
                                                               writeEnabled: config.WriteEnabled,
                                                               shareDenyRead: true,
                                                               shareDenyWrite: true,
                                                               password: "pswd",
                                                               encryption: SolFS.SolFSEncryption.ecAES256_SHA256,
                                                               reseved: 0))
                    {
                        byte[] buffer = new byte[1024 * 1024];
                        long ToRead = 0;
                        while (fileStream.Position < fileStream.Length)
                        {
                            if (fileStream.Length - fileStream.Position < 1024 * 1024)
                                ToRead = fileStream.Length - fileStream.Position;
                            else
                                ToRead = 1024 * 1024;
                            fileStream.Read(buffer, 0, (int)ToRead);
                            solFSStream.Write(buffer, 0, (int)ToRead);
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }

            }
        }


        public byte[] OpenFile(string path, string fileName)
        {
            byte[] buffer = default(byte[]);

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string fullName = GetFullFileName(path, fileName);
                var config = new SolFSStreamModeConfig(SolFSStreamMode.Read);

                try
                {
                    using (SolFS.SolFSStream solFSStream = new SolFS.SolFSStream(storage: Storage,
                                                               fileName: fullName,
                                                               createNew: config.CreateNew,
                                                               readEnabled: config.ReadEnabled,
                                                               writeEnabled: config.WriteEnabled,
                                                               shareDenyRead: true,
                                                               shareDenyWrite: true,
                                                               password: "pswd",
                                                               encryption: SolFS.SolFSEncryption.ecAES256_SHA256,
                                                               reseved: 0))
                    {

                        using (MemoryStream fileStream = new MemoryStream())
                        {
                            buffer = new byte[1024 * 1024];
                            long ToRead = 0;
                            while (fileStream.Position < fileStream.Length)
                            {
                                if (fileStream.Length - fileStream.Position < 1024 * 1024)
                                    ToRead = fileStream.Length - fileStream.Position;
                                else
                                    ToRead = 1024 * 1024;
                                fileStream.Read(buffer, 0, (int)ToRead);
                                solFSStream.Write(buffer, 0, (int)ToRead);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }

            }

            return buffer;
        }

        public void RenameFile(string path, string oldFileName, string newFileName)
        {
            if (!string.IsNullOrWhiteSpace(oldFileName) && !string.IsNullOrWhiteSpace(newFileName))
            {
                string oldFullName = GetFullFileName(path, oldFileName);
                string newFullName = GetFullFileName(path, newFileName);

                Storage.MoveFile(
                    oldFileName: oldFullName,
                    newFileName: newFullName);
            }
        }
        private string GetFullFileName(string path, string fileName)
        {
            path = string.IsNullOrWhiteSpace(path) ? "\\" : path;

            var fullName = string.Format(@"{0}\{1}", path, fileName);

            if (fullName.StartsWith("\\\\"))
                fullName = fullName.Remove(0, 1);

            if (FileExists(fullName))
                throw new Exception("File Exists");
            return fullName;
        }

        protected void Sample_CalculateHash(SolFSStorage Sender, byte[] buffer, byte[] hashBuffer, ref Int32 Result)
        {
            Result = 0;
            for (int i = 0; i < hashBuffer.Length; i++)
                hashBuffer[i] = 0;
        }

        protected void Sample_ValidateHash(SolFSStorage Sender, byte[] buffer, byte[] hashBuffer, ref System.Boolean Valid, ref Int32 Result)
        {
            Result = 0;
            Valid = true;
        }

        protected void Sample_EncryptData(SolFSStorage Sender, byte[] key, byte[] data, uint ObjectID, uint PageIndex, ref Int32 Result)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] ^= key[i % key.Length];
            Result = 0;
        }

        protected void Sample_DecryptData(SolFSStorage Sender, byte[] key, byte[] data, uint ObjectID, uint PageIndex, ref Int32 Result)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] ^= key[i % key.Length];
            Result = 0;
        }	


    }
}