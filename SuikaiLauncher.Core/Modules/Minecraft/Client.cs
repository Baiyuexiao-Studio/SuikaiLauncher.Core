using System.Text.Json.Nodes;
using System.Text;
using SuikaiLauncher.Core.Runtime.Java;
using SuikaiLauncher.Core.Override;
using Exceptions;
using System.Threading.Tasks;
using SuikaiLauncher.Core.Base;
using System.Runtime.Versioning;
using System.Reflection.Metadata.Ecma335;
using System.Net.NetworkInformation;

namespace SuikaiLauncher.Core.Minecraft {
    /// <summary>
    /// ����������
    /// </summary>
    public enum ModLoaderType{
        Unavailable = 0,
        Forge = 1,
        Fabric = 2,
        OptiFine = 3,
        NeoForge = 4,
        Quilt = 5,
        LiteLoader = 6,
        Mixin = 7
    }
    /// <summary>
    /// �汾����
    /// </summary>
    public enum VersionType{

    }
    /// <summary>
    /// �汾Դ����
    /// </summary>
    public class McVersion
    {
        /// <summary>
        /// Minecraft �汾
        /// </summary>
        public string? Version { get; set; }
        /// <summary>
        /// �汾 Json �����ص�ַ
        /// </summary>
        public string? JsonUrl { get; set; }
        /// <summary>
        /// ����� Java
        /// </summary>
        public JavaProperty? RequireJava { get; set; }
        /// <summary>
        /// ��������
        /// </summary>
        public string? VersionName { get; set; }
        /// <summary>
        /// �Ƿ�ɰ�װ Mod
        /// </summary>
        public bool Modable;
        /// <summary>
        /// Mod ����������
        /// </summary>
        public ModLoaderType ModLoader { get; set; }
        /// <summary>
        /// ��װ�˰汾�� Minecraft �ļ���
        /// </summary>
        public string? MinecraftFloder {get;set;}
        /// <summary>
        /// �汾 Json �ļ� Hash
        /// </summary>
        public string? JsonHash;
        /// <summary>
        /// �汾����ʱ��
        /// </summary>
        public DateTime? ReleaseTime;
        /// <summary>
        /// ���ݰ汾�Ų��Ҷ�Ӧ�İ汾
        /// </summary>
        /// <returns>һ�� bool ����ָʾ�Ƿ���ҵ���Ӧ�汾</returns>
        public bool Lookup()
        {
            return false;
        }
        /// <summary>
        /// ���ݰ汾���Ʋ��ұ��ذ汾
        /// </summary>
        /// <returns>һ�� bool ����ָʾ�Ƿ��ڱ����ҵ��˰汾</returns>
        public bool LocalLookup(){
            return false;
        }

    }
    /// <summary>
    /// ��װ�ͻ���
    /// </summary>
    public class Client
    {
        
        public async static void InstallRequest(McVersion Version)
        {
            string? RawJson;
            JsonNode? VersionJson;
            List<Download.FileMetaData> DownloadList =new();
            // ����������飨�汾�Ѵ��ڻ��߹ؼ�����Ϊ�ջ� null��
            if (Directory.Exists($"{Version.MinecraftFloder}/versions/{Version.VersionName}") && File.Exists($"{Version.MinecraftFloder}/versions/{Version.VersionName}/{Version.VersionName}.json")) throw new OperationCanceledException("�汾�Ѵ���");
            if (Version.MinecraftFloder.IsNullOrWhiteSpaceF() || Version.JsonUrl.IsNullOrWhiteSpaceF()) throw new NullReferenceException("ָ����������һ������Ϊ�ջ� null");
            if (Version.MinecraftFloder.EndsWith("/")) Version.MinecraftFloder = Version.MinecraftFloder.TrimEnd('/');
            if (Version.VersionName.IsNullOrWhiteSpaceF()) Version.VersionName = Version.Version;
            Logger.Log($"[Minecraft] ��ʼ��װ Minecraft {Version.Version}");
            Logger.Log("[Minecraft] ========== ����Ԫ���� ==========");
            Logger.Log($"[Minecraft] �������ƣ�{Version.VersionName}");
            Logger.Log($"[Minecraft] �汾��{Version.Version}");
            Logger.Log($"[Minecraft] �ɰ�װ Mod�� {((Version.Modable)? true:false)}");
            Logger.Log($"[Minecraft] ���������{nameof(Version.ModLoader)}");
            Logger.Log($"[Minecraft] Ҫ��� Java �汾��{Version.RequireJava.MojarVersion}");
            Logger.Log($"[Minecraft] ��װ·����");
            RawJson = await DownloadVersionJson(Version,$"{Version.MinecraftFloder}/versions/{Version.VersionName}/{Version.VersionName}.json");
            VersionJson = Json.GetJson(RawJson);
            if (VersionJson is null) throw new InvalidDataException();
            Tuple<List<Download.FileMetaData>,List<Download.FileMetaData>> MetaData = await GetMinecraftLib(VersionJson,Version.MinecraftFloder,RawJson.ContainsF("classifiers"));
            DownloadList.AddRange(MetaData.Item1);
            DownloadList.AddRange(MetaData.Item2);
            DownloadList.AddRange(await GetMinecraftAssets(VersionJson));
            Download.Start(DownloadList);
        }
        public async static Task<string>? DownloadVersionJson(McVersion version, string path,bool RequireOutputOnly = false)
        {
            if (Directory.Exists(path))
            {
                byte[] Result = await Download.NetGetFileByClient(new Download.FileMetaData(){
                    url = version.JsonUrl
                    hash = 
                });
                if (Result != null)
                {
                    string RawJson = Encoding.UTF8.GetString(Result);
                    if (!RequireOutputOnly)
                    {

                    }
                    return RawJson;
                }
            }
            throw new FileNotFoundException("ָ����·��������");
        }
        public async static Task<Tuple<List<Download.FileMetaData>,List<Download.FileMetaData>>> GetMinecraftLib(JsonNode VersionJson,string MinecraftFolder,bool OldVersionInstallMethod = false) 
        {
            List<Download.FileMetaData> CommonLib = new();
            List<Download.FileMetaData> NativesLib = new();
            try
            {
                // �ɰ汾�� classifiers ���°汾û�У�Ϊ�˱���ϲ���װ�������µ� Bug �Ͷ��⹤�������������������д
                if (OldVersionInstallMethod)
                {
                    foreach(JsonNode libaray in VersionJson["libraries"]?.GetValue<List<JsonNode>>())
                    {
                        if (libaray is null) throw new InvalidDataException("Json �ṹ��Ч");
                        JsonNode? artifact = libaray["downloads"]?["artifact"];
                        JsonNode? classifier = libaray["downloads"]?["classifiers"];
                        if (artifact is not null) {
                            Download.FileMetaData MetaData = new(){
                                url = (string?)artifact["url"],
                                hash = (string?)artifact["sha1"],
                                algorithm = "sha1",
                                path = $"{MinecraftFolder}/libraries/{artifact["path"]}",
                                size = (long?)artifact["size"]
                            };
                            CommonLib.Add(MetaData);
                        }
                        if (classifier is not null){
                            // ���ݲ�ͬ����ϵͳ����Ҫ���ص�֧�ֿ�
                            switch (Environments.SystemType){
                                case Environments.OSType.Windows:
                                    Download.FileMetaData WinLibMetaData = new(){
                                        url = (string?)classifier["natives-windows"]?["url"],
                                        hash = (string?)classifier["natives-windows"]?["sha1"],
                                        size = (long?)classifier["natives-windows"]?["size"],
                                        path = $"{MinecraftFolder}/libraries/{(string?)classifier["natives-windows"]?["path"]}",
                                        algorithm = "sha1"
                                    };
                                    NativesLib.Add(WinLibMetaData);
                                    break;
                                case Environments.OSType.Linux:
                                    Download.FileMetaData LnxLibMetaData = new(){
                                        url = (string?)classifier["natives-linux"]?["url"],
                                        hash = (string?)classifier["natives-linux"]?["sha1"],
                                        size = (long?)classifier["natives-linux"]?["size"],
                                        path = $"{MinecraftFolder}/libraries/{(string?)classifier["natives-linux"]?["path"]}",
                                        algorithm = "sha1"
                                    };
                                    NativesLib.Add(LnxLibMetaData);
                                    break;
                                case Environments.OSType.MacOS:
                                    Download.FileMetaData OsxLibMetaData = new(){
                                        url = (string?)classifier["natives-osx"]?["url"],
                                        hash = (string?)classifier["natives-osx"]?["sha1"],
                                        size = (long?)classifier["natives-osx"]?["size"],
                                        path = $"{MinecraftFolder}/libaraies/{(string?)classifier["natives-osx"]?["path"]}",
                                        algorithm = "sha1"
                                    };
                                    NativesLib.Add(OsxLibMetaData);
                                    break;
                                    // FreeBSD �Լ���������ϵͳ
                                    // ������֪��Ҫ��ʲô��
                                case Environments.OSType.FreeBSD | Environments.OSType.Unknown:
                                    throw new NotImplementedException("�ݲ�֧�ִ�ƽ̨");
                            }
                        }

                    }
                    return Tuple.Create(CommonLib,NativesLib);
                }
                // �°汾��װ����
                foreach (JsonNode? library in VersionJson["libraries"].GetValue<List<JsonNode>>()){
                    if (library is null) throw new InvalidDataException("Json �ṹ��Ч");
                    JsonNode? artifact = library["downloads"];
                    // natives �ļ�
                    if (artifact["url"] is not null || artifact["url"].ToString().ContainsF("native")){
                        // �ų��ܹ������֧�ֿ�
                        if (artifact["url"].ToString().ContainsF("arm") && (Environments.SystemArch != System.Runtime.InteropServices.Architecture.Arm || Environments.SystemArch != System.Runtime.InteropServices.Architecture.Arm64)) continue;
                        // ���� url �ж�֧�ֿ����õĲ���ϵͳ
                        if (artifact["url"].ToString().ContainsF("windows") && Environments.OSType.Windows == Environments.SystemType){
                            Download.FileMetaData NativeLib = new(){
                                url = (string?)artifact["url"],
                                path = $"{MinecraftFolder}/libraries/{(string?)artifact["path"]}",
                                hash = (string?)artifact["sha1"],
                                size = (long?)artifact["size"],
                                algorithm = "sha1"
                            };
                            NativesLib.Add(NativeLib);
                        }
                        else if (artifact["url"].ToString().ContainsF("linux") && Environments.OSType.Linux == Environments.SystemType){
                            Download.FileMetaData NativeLib = new(){
                                url = (string?)artifact["url"],
                                path = $"{MinecraftFolder}/libraries/{(string?)artifact["path"]}",
                                hash = (string?)artifact["sha1"],
                                size = (long?)artifact["size"],
                                algorithm = "sha1"
                            };
                            NativesLib.Add(NativeLib);
                        }
                        else if ((artifact["url"].ToString().ContainsF("osx") ||artifact["url"].ToString().ContainsF("macos")) && Environments.OSType.MacOS == Environments.SystemType){
                            Download.FileMetaData NativeLib = new(){
                                url = (string?)artifact["url"],
                                path = $"{MinecraftFolder}/libraries/{(string?)artifact["path"]}",
                                hash = (string?)artifact["sha1"],
                                size = (long?)artifact["size"],
                                algorithm = "sha1"
                            };
                            NativesLib.Add(NativeLib);
                        }
                        // ��Ȼ��֪������ʲô����ϵͳ����������˵
                        else {
                            Download.FileMetaData NativeLib = new(){
                                url = (string?)artifact["url"],
                                path = $"{MinecraftFolder}/libraries/{(string?)artifact["path"]}",
                                hash = (string?)artifact["sha1"],
                                size = (long?)artifact["size"],
                                algorithm = "sha1"
                            };
                            NativesLib.Add(NativeLib);
                        }
                    }
                    CommonLib.Add(new Download.FileMetaData(){
                        url = (string?)artifact["url"],
                        path = $"{MinecraftFolder}/libraries/{(string?)artifact["path"]}",
                        hash = (string?)artifact["sha1"],
                        size = (long?)artifact["size"],
                        algorithm = "sha1"
                    });
                }
                return Tuple.Create(NativesLib,CommonLib);
            }
            catch (TaskCanceledException)
            {
                Logger.Log("��װ������ȡ��");
            }
            catch(Exception ex)
            {
                Logger.Log(ex, "��ȡ֧�ֿ��б�ʧ��");
                throw;
            }
            throw new Exception("δ֪����");
        }
        public async static Task<List<Download.FileMetaData>> GetMinecraftAssets(JsonNode VersionJson) {
            JsonNode? Result = Download.NetGetFileByClient()
        }
            
    }
    public class Server
    {
        public async static Task DownloadServerCore(string Version)
        {
            
        }
    }
}