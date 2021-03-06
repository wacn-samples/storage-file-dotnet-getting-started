---
services: storage
platforms: dotnet
author: robinsh

---

# .NET中使用Azure文件服务起步

演示如何使用文件存储服务。

注意：这个示例使用.NET 4.5异步编程模型来演示如何使用存储客户端库的异步API调用存储服务。 在实际的应用中这种方式可以提高程序的响应速度。存储服务需要在调用时前面添加关键字await。如果您还没有Azure订阅，请点击[此处](/pricing/1rmb-trial)申请试用的订阅账号。


##运行这个示例

这个示例可以在Azure存储模拟器（存储模拟器是Azure SDK安装的一部分）上运行，或者通过修改App.Config文档中的AccountName（存储账号）和Key（存储密钥）的方式来使用。   
        
使用Azure存储服务来运行这个示例

2. 在Azure门户网站上创建存储账号，然后修改App.Config的 [AccountName]（存储账号）和 [AccountKey]（存储密钥）。
3. 设置断点，使用F10运行该示例。

##参考文档: 

- [什么是存储账号](/documentation/articles/storage-create-storage-account/)
- [文件服务起步](http://blogs.msdn.com/b/windowsazurestorage/archive/2014/05/12/introducing-microsoft-azure-file-service.aspx)
- [如何使用文件服务](/documentation/articles/storage-dotnet-how-to-use-files/)
- [文件服务概念](http://msdn.microsoft.com/zh-cn/library/dn166972.aspx)
- [文件服务 REST API](http://msdn.microsoft.com/zh-cn/library/dn167006.aspx)
- [文件服务 C# API](https://msdn.microsoft.com/zh-cn/library/microsoft.windowsazure.storage.file.aspx)
- [使用 Async 和 Await异步编程](http://msdn.microsoft.com/zh-cn/library/hh191443.aspx)

