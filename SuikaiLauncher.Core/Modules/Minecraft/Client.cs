using System.Text.Json.Nodes;
using System.Text;
using SuikaiLauncher.Core.Runtime.Java;
using SuikaiLauncher.Core.Override;
using SuikaiLauncher.Core.Base;

namespace SuikaiLauncher.Core.Minecraft {
    /// <summary>
    /// ����������
    /// </summary>
    
    /// <summary>
    /// �汾����
    /// </summary>
    public enum VersionType
    {
        Release = 0,
        Snapshot = 1,
        Special = 2,
        Old = 3
    }
    public class Subassembly
    {
        public string? ForgeVersion { get; set; }
        public string? FabricVersion { get; set; }
        public string? NeoForgeVersion { get; set; }
        public string? QuiltVersion { get; set; }
        public string? CleanroomVersion { get; set; }
        public string? OptiFineVersion { get; set; }
        public string? LiteLoaderVersion { get; set; }
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
        /// ���������Ϣ
        /// </summary>
        public Subassembly? SubassemblyMeta { get ; set; }
        /// <summary>
        /// ��װ�˰汾�� Minecraft �ļ���
        /// </summary>
        public string? MinecraftFolder {get;set;}
        /// <summary>
        /// �汾 Json �ļ� Hash
        /// </summary>
        public string? JsonHash { get; set; }
        /// <summary>
        /// �汾����ʱ��
        /// </summary>
        public DateTime? ReleaseTime { get; set; }

        public VersionType Type { get; set; }
        /// <summary>
        /// ��װĿ¼
        /// </summary>
        public string? InstallFolder;
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
            if (Version.VersionName.IsNullOrWhiteSpaceF()) Version.VersionName = Version.Version;
            if (Directory.Exists($"{Version.MinecraftFolder}/versions/{Version.VersionName}") && File.Exists($"{Version.MinecraftFolder}/versions/{Version.VersionName}/{Version.VersionName}.json")) throw new OperationCanceledException("�汾�Ѵ���");
            if (Version.MinecraftFolder.IsNullOrWhiteSpaceF() || Version.JsonUrl.IsNullOrWhiteSpaceF()) throw new NullReferenceException("ָ����������һ������Ϊ�ջ� null");
            if (Version.MinecraftFolder.EndsWith("/")) Version.MinecraftFolder = Version.MinecraftFolder.TrimEnd('/');
            Logger.Log($"[Minecraft] ��ʼ��װ Minecraft {Version.Version}");
            Logger.Log("[Minecraft] ========== ����Ԫ���� ==========");
            Logger.Log($"[Minecraft] �������ƣ�{Version.VersionName}");
            Logger.Log($"[Minecraft] �汾��{Version.Version}");
            Logger.Log($"[Minecraft] �汾���ͣ�{Enum.GetName(Version.Type)}");
            Logger.Log($"[Minecraft] �ɰ�װ Mod�� {Version.SubassemblyMeta is not null}");
            Logger.Log($"[Minecraft] Ҫ��� Java �汾�� {( (Version.RequireJava is not null) ? $"Java {Version.RequireJava.MojarVersion}":"δָ��" )}");
            Logger.Log($"[Minecraft] ��װ·����{Version.InstallFolder}");
            Logger.Log("[Minecraft] ===========================");
            
            RawJson = await DownloadVersionJson(Version,$"{Version.MinecraftFolder}/versions/{Version.VersionName}/{Version.VersionName}.json");
            VersionJson = Json.GetJson(RawJson);
            if (VersionJson is null) throw new InvalidDataException();
            Tuple<List<Download.FileMetaData>,List<Download.FileMetaData>> MetaData = await GetMinecraftLib(VersionJson,Version,RawJson.ContainsF("classifiers"));
            DownloadList.AddRange(MetaData.Item1);
            DownloadList.AddRange(MetaData.Item2);
            DownloadList.AddRange(await GetMinecraftAssets(VersionJson,Version,Version.Type == VersionType.Old));
            if (!VersionJson["downloads"].ToString().IsNullOrWhiteSpaceF())
            {
                DownloadList.Add(new Download.FileMetaData()
                {
                    url = VersionJson["downloads"]?["client"]?["url"]?.ToString(),
                    path = $"{Version.InstallFolder}/{Version.VersionName}.jar",
                    hash = VersionJson["downloads"]?["client"]?["sha1"]?.ToString(),
                    algorithm = "sha1",
                    size = (long?)VersionJson["downloads"]?["client"]?["size"]
                });
            }
            // Download.Start(DownloadList);
            
        }
        public async static Task<string>? DownloadVersionJson(McVersion version, string path,bool RequireOutputOnly = false)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                
                string Result = await Download.NetGetFileByClient(new Download.FileMetaData(){
                    url = version.JsonUrl,
                    hash =  version.JsonHash
                });
                if (Result != null)
                {
                    
                    if (!RequireOutputOnly)
                    {
                        File.WriteAllBytes(path,Encoding.UTF8.GetBytes(Result));
                    }
                    return Result;
                }
                else
                {
                    throw new ArgumentException("�����ļ�ʧ��");
                }
            }
            throw new FileNotFoundException("ָ����·���Ѵ���");
        }
        public async static Task<Tuple<List<Download.FileMetaData>,List<Download.FileMetaData>>> GetMinecraftLib(JsonNode VersionJson,McVersion Version,bool OldVersionInstallMethod = false) 
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
                                path = $"{Version.MinecraftFolder}/libraries/{artifact["path"]}",
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
                                        path = $"{Version.MinecraftFolder}/libraries/{(string?)classifier["natives-windows"]?["path"]}",
                                        algorithm = "sha1"
                                    };
                                    NativesLib.Add(WinLibMetaData);
                                    break;
                                case Environments.OSType.Linux:
                                    Download.FileMetaData LnxLibMetaData = new(){
                                        url = (string?)classifier["natives-linux"]?["url"],
                                        hash = (string?)classifier["natives-linux"]?["sha1"],
                                        size = (long?)classifier["natives-linux"]?["size"],
                                        path = $"{Version.MinecraftFolder}/libraries/{(string?)classifier["natives-linux"]?["path"]}",
                                        algorithm = "sha1"
                                    };
                                    NativesLib.Add(LnxLibMetaData);
                                    break;
                                case Environments.OSType.MacOS:
                                    Download.FileMetaData OsxLibMetaData = new(){
                                        url = (string?)classifier["natives-osx"]?["url"],
                                        hash = (string?)classifier["natives-osx"]?["sha1"],
                                        size = (long?)classifier["natives-osx"]?["size"],
                                        path = $"{Version.MinecraftFolder}/libaraies/{(string?)classifier["natives-osx"]?["path"]}",
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
                    if (artifact is not null && artifact["url"] is not null && artifact["url"].ToString().ContainsF("native")){
                        // �ų��ܹ������֧�ֿ�
                        if (artifact["url"].ToString().ContainsF("arm") && (Environments.SystemArch != System.Runtime.InteropServices.Architecture.Arm || Environments.SystemArch != System.Runtime.InteropServices.Architecture.Arm64)) continue;
                        // ���� url �ж�֧�ֿ����õĲ���ϵͳ
                        if (artifact["url"].ToString().ContainsF("windows") && Environments.OSType.Windows == Environments.SystemType){
                            Download.FileMetaData NativeLib = new(){
                                url = (string?)artifact["url"],
                                path = $"{Version.MinecraftFolder}/libraries/{(string?)artifact["path"]}",
                                hash = (string?)artifact["sha1"],
                                size = (long?)artifact["size"],
                                algorithm = "sha1"
                            };
                            NativesLib.Add(NativeLib);
                        }
                        else if (artifact["url"].ToString().ContainsF("linux") && Environments.OSType.Linux == Environments.SystemType){
                            Download.FileMetaData NativeLib = new(){
                                url = (string?)artifact["url"],
                                path = $"{Version.MinecraftFolder}/libraries/{(string?)artifact["path"]}",
                                hash = (string?)artifact["sha1"],
                                size = (long?)artifact["size"],
                                algorithm = "sha1"
                            };
                            NativesLib.Add(NativeLib);
                        }
                        else if ((artifact["url"].ToString().ContainsF("osx") ||artifact["url"].ToString().ContainsF("macos")) && Environments.OSType.MacOS == Environments.SystemType){
                            Download.FileMetaData NativeLib = new(){
                                url = (string?)artifact["url"],
                                path = $"{Version.MinecraftFolder}/libraries/{(string?)artifact["path"]}",
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
                                path = $"{Version.MinecraftFolder}/libraries/{(string?)artifact["path"]}",
                                hash = (string?)artifact["sha1"],
                                size = (long?)artifact["size"],
                                algorithm = "sha1"
                            };
                            NativesLib.Add(NativeLib);
                        }
                    }
                    CommonLib.Add(new Download.FileMetaData(){
                        url = (string?)artifact["url"],
                        path = $"{Version.MinecraftFolder}/libraries/{(string?)artifact["path"]}",
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
        public async static Task<List<Download.FileMetaData>> GetMinecraftAssets(JsonNode VersionJson,McVersion Version,bool CopyToResource) {
            List<Download.FileMetaData> DownloadList = new();
            string? VersionAssetsUrl = VersionJson["assetsIndex"]?["url"]?.ToString();
            int? VersionAssetsSize = (int?)VersionJson["assetsIndex"]?["size"];
            string? VersionAssetsHash = VersionJson["assetsIndex"]?["sha1"]?.ToString();
            string? VersionAssetsIndex = VersionJson["assetsIndex"]?["id"]?.ToString();
            if (VersionAssetsUrl.IsNullOrWhiteSpaceF() || VersionAssetsSize is not null || VersionAssetsHash.IsNullOrWhiteSpaceF()) throw new ArgumentException("��Ч�İ�װԪ����");
            string Result = await Download.NetGetFileByClient(new Download.FileMetaData()
            {
                url = VersionAssetsUrl,
                size = VersionAssetsSize,
                hash = VersionAssetsHash,
                algorithm = "sha1",
                path = $"{Version.MinecraftFolder}/assets/indexes/{VersionAssetsIndex}.json"
            });
            JsonNode? AssetsJson = Json.GetJson(Result);
            JsonNode? AssetsObject = AssetsJson?["objects"];
            if (AssetsObject is not null)
            {
                foreach (var Resource in AssetsObject as JsonObject)
                {
                    string ResHash = Resource.Value["sha1"]?.ToString();
                    DownloadList.Add(new Download.FileMetaData()
                    {
                        url = Source.GetResourceDownloadSource(Resource.Value["sha1"]?.ToString()),
                        path = (CopyToResource) ? $"{Version.MinecraftFolder}/resource/{Resource.Key}" : $"{Version.MinecraftFolder}/assets/{ResHash.Substring(0, 2)}/{ResHash}",
                        hash = ResHash,
                        algorithm = "sha1",
                        size = (long?)Resource.Value["size"]
                    });
                }
                return DownloadList;
            }
            throw new ArgumentException("�汾 Json �ļ��������⣬�޷���װ��");
        }
            
    }
    public class Server
    {
        public async static Task DownloadServerCore(string Version)
        {
            
        }
    }
}