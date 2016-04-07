//----------------------------------------------------------------------------------
// Microsoft Azure Storage Team
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//----------------------------------------------------------------------------------

namespace DataFileStorageSample
{
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.File;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Azure 文件存储示例 - 演示如何使用文件存储服务
    /// 
    /// 注意：这个示例使用.NET 4.5异步编程模型来演示如何使用存储客户端库的异步API调用存储服务。 在实际的应用中这种方式
    /// 可以提高程序的响应速度。调用存储服务只要添加关键字await为前缀即可。
    /// 
    /// 参考文档: 
    /// - 什么是存储账号- https://www.azure.cn/documentation/articles/storage-create-storage-account/
    /// - 文件服务起步 - http://blogs.msdn.com/b/windowsazurestorage/archive/2014/05/12/introducing-microsoft-azure-file-service.aspx
    /// - 如何使用文件服务 - https://www.azure.cn/documentation/articles/storage-dotnet-how-to-use-files/
    /// - 文件服务概念 - http://msdn.microsoft.com/zh-cn/library/dn166972.aspx
    /// - 文件服务 REST API - http://msdn.microsoft.com/zh-cn/library/dn167006.aspx
    /// - 文件服务 C# API - https://msdn.microsoft.com/zh-cn/library/microsoft.windowsazure.storage.file.aspx
    /// - 使用 Async 和 Await异步编程  - http://msdn.microsoft.com/zh-cn/library/hh191443.aspx
    /// </summary>
    public class Program
    {
        // *************************************************************************************************************************
        // 使用说明: 这个示例可以通过修改App.Config文档中的存储账号和存储密匙的方式针对存储服务来使用。   
        //   
        // 使用Azure存储服务来运行这个示例
        //      1. 在Azure门户网站上创建存储账号，然后修改App.Config的存储账号和存储密钥。更多详细内容请阅读：https://www.azure.cn/documentation/articles/storage-dotnet-how-to-use-files/
        //      2. 设置断点，然后使用F10按钮运行这个示例. 
        // 
        // *************************************************************************************************************************        
        static void Main(string[] args)
        {
            Console.WriteLine("Azure 文件存储示例\n ");

            // 创建共享、上传文件、下载文件、列出文件和文件夹、复制文件、终止复制文件、写入范围、列出范围
            RunFileStorageOperationsAsync().Wait();

            Console.WriteLine("按任意键退出");
            Console.ReadLine();
        }

        /// <summary>
        /// 测试一些文件存储的操作
        /// </summary>
        private static async Task RunFileStorageOperationsAsync() 
        {
            try
            {
                //***** 设定 *****//
                Console.WriteLine("获取存储账号的引用.");

                // 通过连接字符串检索存储账号的信息
                CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));

                Console.WriteLine("实例化文件客户端.");

                // 创建一个文件客户端用于和文件服务交互
                CloudFileClient cloudFileClient = storageAccount.CreateCloudFileClient();

                // 创建共享名 -- 使用guid作为名称的一部分，这样可以确保唯一性
                // 这同时也适用于后面创建的存储容器的名字
                string shareName = "demotest-" + System.Guid.NewGuid().ToString().Substring(0, 12);

                // 放置文件的文件夹的名字
                string sourceFolder = "testfolder";

                // 上传下载文件的名字 
                string testFile = "HelloWorld.png";

                // HelloWorld.png存在的文件夹 
                string localFolder = @".\";

                // 它不会让你下载到和exe文件同样文件夹中，所以使用一个和共享一样名字的临时文件夹
                string downloadFolder = Path.Combine(Path.GetTempPath(), shareName);

                //***** 创建一个文件共享 *****//

                // 在共享不存时创建
                Console.WriteLine("创建的共享名： {0}", shareName);
                CloudFileShare cloudFileShare = cloudFileClient.GetShareReference(shareName);
                try
                {
                    await cloudFileShare.CreateIfNotExistsAsync();
                    Console.WriteLine("   共享创建成功.");
                }
                catch (StorageException exStorage)
                {
                    WriteException(exStorage);
                    Console.WriteLine("请确保你的存储账号启用了文件存储终结点，并在app.config中设置正确的值 - 然后重新运行该示例.");
                    Console.WriteLine("按任意键退出");
                    Console.ReadLine();
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  创建共享过程中抛出错误.");
                    WriteException(ex);
                    throw;
                }

                //***** 在文件共享上创建目录 *****//

                // 在共享上创建目录.
                Console.WriteLine("创建的目录名： {0}", sourceFolder);

                // 首先，获取根目录引用，这是您创建新目录的地方
                CloudFileDirectory rootDirectory = cloudFileShare.GetRootDirectoryReference();
                CloudFileDirectory fileDirectory = null;

                // 设置文件目录的引用
                // 如果源文件夹为null，然后使用根文件夹
                // 如果已经指定源文件夹，就得到它的引用
                if (string.IsNullOrWhiteSpace(sourceFolder))
                {
                    // 文件夹未指定，所以返回根目录的引用
                    fileDirectory = rootDirectory;
                    Console.WriteLine("使用根目录.");
                }
                else
                {
                    // 已指定文件夹，所以返回文件夹的引用
                    fileDirectory = rootDirectory.GetDirectoryReference(sourceFolder);

                    await fileDirectory.CreateIfNotExistsAsync();
                    Console.WriteLine(" 目录创建成功");
                }

                //***** 上传文件到文件共享 *****//

                // 设置文件的引用
                CloudFile cloudFile = fileDirectory.GetFileReference(testFile);

                // 上传文件到共享
                Console.WriteLine("Uploading file {0} to share", testFile);

                // 设置本地文件的名字和路径
                string sourceFile = Path.Combine(localFolder, testFile);
                if (File.Exists(sourceFile))
                {
                    // 上传本地文件到Azure的文件共享
                    await cloudFile.UploadFromFileAsync(sourceFile, FileMode.OpenOrCreate);
                    Console.WriteLine("上传文件到共享成功.");
                }
                else
                {
                    Console.WriteLine("文件未找到，所以没有上传.");
                }

                //***** 获取并列出文件共享中的文件/目录*****//

                // 列出根目录下的所有的文件/目录.
                Console.WriteLine("列出共享根目录下的所有的文件/目录.");

                IEnumerable<IListFileItem> fileList = cloudFileShare.GetRootDirectoryReference().ListFilesAndDirectories();

                // 打印上面列出的文件/目录
                foreach (IListFileItem listItem in fileList)
                {
                    // listItem 的类型可能是 CloudFile 或者 CloudFileDirectory.
                    Console.WriteLine("    - {0} (type: {1})", listItem.Uri, listItem.GetType());
                }

                Console.WriteLine("获取共享中文件目录下所有的文件/目录.");

                // 现在获得您目录下的所有文件/目录
                // 通常，你需要使用递归来列出所有的目录和子目录

                fileList = fileDirectory.ListFilesAndDirectories();

                // 打印文件夹下所有文件/目录
                foreach (IListFileItem listItem in fileList)
                {
                    // listItem 的类型可能是 CloudFile 或者 CloudFileDirectory.
                    Console.WriteLine("    - {0} (类型: {1})", listItem.Uri, listItem.GetType());
                }

                //***** 从文件共享下载文件 *****//

                // 下载文件到临时目录的downloadFolder文件夹中
                // 检查，如果目录不存在，则创建
                Console.WriteLine("从共享下载文件到本地的临时文件夹 {0}.", downloadFolder);
                if (!Directory.Exists(downloadFolder))
                {
                    Directory.CreateDirectory(downloadFolder);
                }

                // 下载文件
                await cloudFile.DownloadToFileAsync(Path.Combine(downloadFolder, testFile), FileMode.OpenOrCreate);
                Console.WriteLine("    从共享下载文件到本地的临时文件夹成功.");

                //***** 从文件共享复制文件到blob存储中，然后终止复制 *****//

                // 为了测试这个，您需要找一个大点的文件，否则在终止复制时复制可能已经完成
                // 你需要上传文件到共享。然后您能够将文件的名字赋值到testFile变量，然后使用这个文件复制和终止复制
                CloudFile cloudFileCopy = fileDirectory.GetFileReference(testFile);

                // 上传文件到文件共享
                Console.WriteLine("上传文件 {0} 到共享", testFile);

                // 设置本地文件名字路径
                string sourceFileCopy = Path.Combine(localFolder, testFile);
                await cloudFileCopy.UploadFromFileAsync(sourceFileCopy, FileMode.OpenOrCreate);
                Console.WriteLine("   上传文件到共享成功.");

                // 复制文件到blob存储中.
                Console.WriteLine("复制文件到blob存储中. 容器名 = {0}", shareName);

                // 首先得到blob的引用
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                // 得到blob容器引用，如果不存在就创建
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(shareName);
                cloudBlobContainer.CreateIfNotExists();

                // 得到目标blob的引用
                CloudBlob targetBlob = cloudBlobContainer.GetBlobReference(testFile);

                string copyId = string.Empty;

                // 获取要复制文件的引用.
                cloudFile = fileDirectory.GetFileReference(testFile);

                // 为文件创建一个持续有效时间24小时的共享访问签名（SAS）
                // 注意当你复制文件到blob，或者blob到文件，您必须SAS来和源对象做验证，即使您在相同的存储账号下复制
                string fileSas = cloudFile.GetSharedAccessSignature(new SharedAccessFilePolicy()
                {
                    // 只给源文件读的权限
                    Permissions = SharedAccessFilePermissions.Read,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24)
                });

                // 构建源文件的URI，包含SAS令牌
                Uri fileSasUri = new Uri(cloudFile.StorageUri.PrimaryUri.ToString() + fileSas);

                // 开始复制文件到blob
                copyId = await targetBlob.StartCopyAsync(fileSasUri);
                Console.WriteLine("  开始复制文件成功. copyID = {0}", copyId);

                // 终止复制文件到blob存储
                // 注意，您可以终止目标对象，例如blob，而不是文件。
                // 如果您在文件共享中正从一个文件复制到另一个，目标对象应该是文件 
                Console.WriteLine("取消复制操作.");

                // 打印出复制状态信息
                targetBlob.FetchAttributes();
                Console.WriteLine("    targetBlob.copystate.CopyId = {0}", targetBlob.CopyState.CopyId);
                Console.WriteLine("    targetBlob.copystate.Status = {0}", targetBlob.CopyState.Status);

                // 真正的终止复制
                // 如果复制是pending或者ongoing的状态才工作
                if (targetBlob.CopyState.Status == CopyStatus.Pending)
                {
                    // 通过传递操作的copyID来停止复制
                    // 如果已经复制完成这将不工作
                    await targetBlob.AbortCopyAsync(copyId);
                    Console.WriteLine("  取消复制成功.");
                }
                else
                {
                    // 如果发生这种情况，请尝试大点的文件
                    Console.WriteLine("   取消复制没有执行；复制已经完成.");
                }

                // 现在自己清理一下
                Console.WriteLine("共文件共享中删除文件.");

                // 删除文件，在范围示例中cloudFile将会不同
                cloudFile = fileDirectory.GetFileReference(testFile);
                cloudFile.DeleteIfExists();

                Console.WriteLine("设置文件来测试WriteRange和ListRanges.");

                //***** 向文件中写入2个范围，然后列出范围 *****//

                // 这个代码是展示给文件的一定范围写入数据，然后列出范围
                // 得到文件的引用然后在一个范围内写入数据
                // 然后写入另一个范围
                // 列出范围

                // 从文件的开始处开始
                long startOffset = 0;

                // 设置目标文件名 -- 这个是在共享文件上准备写入的
                string destFile = "rangeops.txt";
                cloudFile = fileDirectory.GetFileReference(destFile);

                // 创建一个512"a"的字符串，用于写入范围
                int testStreamLen = 512;
                string textToStream = string.Empty;
                textToStream = textToStream.PadRight(testStreamLen, 'a');

                // 下载时所使用到的文件的名字，所以您可以在本地检查下
                string downloadFile;

                using (MemoryStream ms = new MemoryStream(Encoding.Default.GetBytes(textToStream)))
                {
                    // 输出文件的最大尺寸；在创建文件是需要指定
                    // 下面是我随意的数据
                    long maxFileSize = 65536;

                    Console.WriteLine("写入第一个范围.");

                    // 设置回到流的最初始，防止已经被读
                    ms.Position = 0;

                    // 如果文件不存在则创建
                    // 已经赋予最大文件尺寸，它大的足以容纳所有的你需要写入的数据，所以不要设置为256k，尝试写入两个256-k的块到文件中
                    if (!cloudFile.Exists())
                    {
                        Console.WriteLine("文件不存在，创建空的文件去写范围数据.");

                        // 创建一个最大尺寸64k的文件
                        await cloudFile.CreateAsync(maxFileSize);
                        Console.WriteLine("    空文件创建成功.");
                    }

                    // 从文件的startOffset位置开始写入这个流，写入的长度为整个流的长度
                    Console.WriteLine("写入一个范围到文件中.");
                    await cloudFile.WriteRangeAsync(ms, startOffset, null);

                    // 下载文件到您的临时目录中，我们可以在本地检查它
                    downloadFile = Path.Combine(downloadFolder, "__testrange.txt");
                    Console.WriteLine("下载文件用于检查.");
                    await cloudFile.DownloadToFileAsync(downloadFile, FileMode.OpenOrCreate);
                    Console.WriteLine("    下载拥有范围数据的用于检查的文件成功.");
                }

                // 现在添加第二个范围，但是不要与第一个相邻，否则它将以一个范围展示，让他们有1000个空格键的距离。当您获取范围的时候，
                // 开始的位置是写入位置前最接近写入位置的512的倍数，结束位置是真实写入位置后最接近真实写入位置的512倍数。
                // 例如，您在2000-3000写入范围，开始的位置是2000之前最靠近2000的512的倍数，这个位置是1536，offset是1535(计数是以0开始的)。
                // 右边的偏移量是3000后最靠近3000的512的倍数，这个位置是3072，offset是3071(计数是以0开始的)           
                Console.WriteLine("准备第二个范围到文件中.");

                startOffset += testStreamLen + 1000; //随机选择的数字

                // 创建一个512个"b"的字符串用于写入范围
                textToStream = string.Empty;
                textToStream = textToStream.PadRight(testStreamLen, 'b');

                using (MemoryStream ms = new MemoryStream(Encoding.Default.GetBytes(textToStream)))
                {

                    ms.Position = 0;

                    // 从文件的startOffset位置开始写入这个流，写入的长度为整个流的长度
                    Console.WriteLine("写入第二个范围到文件.");
                    await cloudFile.WriteRangeAsync(ms, startOffset, null);
                    Console.WriteLine("   写入第二个范围到文件成功.");

                    // 下载文件到您的临时目录中用于检查
                    downloadFile = Path.Combine(downloadFolder, "__testrange2.txt");
                    Console.WriteLine("下载两个范围的文件用于检查.");
                    await cloudFile.DownloadToFileAsync(downloadFile, FileMode.OpenOrCreate);
                    Console.WriteLine("    用于检查的文件下载成功.");
                }

                // 查询并查看列出的范围
                Console.WriteLine("调用获取范围的集合.");
                IEnumerable<FileRange> listOfRanges = await cloudFile.ListRangesAsync();
                Console.WriteLine("    检测范围的集合成功.");
                foreach (FileRange fileRange in listOfRanges)
                {
                    Console.WriteLine("    --> filerange startOffset = {0}, endOffset = {1}", fileRange.StartOffset, fileRange.EndOffset);
                }

                //***** 清理 *****//

                // 一些清理工作.
                Console.WriteLine("移除所有示例用到的文件、文件夹、共享、blobs、容器");

                // 删除拥有范围数据的文件.
                cloudFile = fileDirectory.GetFileReference(destFile);
                await cloudFile.DeleteIfExistsAsync();

                Console.WriteLine("删除文件共享中的目录.");

                // 删除目录.
                bool success = false;
                success = await fileDirectory.DeleteIfExistsAsync();
                if (success)
                {
                    Console.WriteLine("    文件共享中的目录删除成功.");
                }
                else
                {
                    Console.WriteLine("    文件共享中的目录没有删除成功；可能不存在.");
                }

                Console.WriteLine("删除文件共享.");

                // 删除共享.
                await cloudFileShare.DeleteAsync();
                Console.WriteLine("    成功删除文件共享");

                Console.WriteLine("删除临时下载目录及其中的文件.");

                // 删除下载文件夹及其中的内容
                Directory.Delete(downloadFolder, true);
                Console.WriteLine("   删除临时下载目录成功.");

                Console.WriteLine("删除用于Copy/Abort的容器和blob.");
                await targetBlob.DeleteIfExistsAsync();
                await cloudBlobContainer.DeleteIfExistsAsync();
                Console.WriteLine("   删除blob和它的容器成功.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("    抛出错误. Message = {0}{1}    Strack Trace = {2}", ex.Message, Environment.NewLine, ex.StackTrace);
            }

        }

        /// <summary>
        /// 验证App.Config文件中的连接字符串，当使用者没有更新有效的值时抛出错误提示
        /// </summary>
        /// <param name="storageConnectionString">连接字符串</param>
        /// <returns>CloudStorageAccount 对象</returns>
        private static CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (FormatException)
            {
                Console.WriteLine("提供的存储信息无效，请确认App.Config文件中的AccountName和AccountKey有效后重新启动该示例");
                Console.ReadLine();
                throw;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("提供的存储信息无效，请确认App.Config文件中的AccountName和AccountKey有效后重新启动该示例");
                Console.ReadLine();
                throw;
            }

            return storageAccount;
        }

        private static void WriteException(Exception ex)
        {
            Console.WriteLine("错误抛出. {0}, msg = {1}", ex.Source, ex.Message);
        }
    }
}
