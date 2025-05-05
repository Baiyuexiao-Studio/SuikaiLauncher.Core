using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using SuikaiLauncher.Core.Modules.Base;
using SuikaiLauncher.Core.Override;

namespace SuikaiLauncher.Core.Account {
    public class Microsoft
    {
        
        public static string ClientId = "";
        private static readonly BrokerOptions options = new(BrokerOptions.OperatingSystems.Windows) { Title = "SuikaiLauncher.Core ��ȫ��¼" };
        private static IPublicClientApplication? OAuthClient;
        private static readonly List<string> Scope = new() { "XboxLive.Signin", "offline_access" };
        private static string OriginId = "";
        private static readonly object MSOAuthLock = new object[1];
        // �豸��������¼��
        public static string? UserCode;
        public static string? VerificationUrl;

        internal static void InitOAuthClient()
        {
            lock (MSOAuthLock)
            {
                if (ClientId.IsNullOrWhiteSpaceF()) throw new ArgumentNullException("Client ID ����Ϊ��");
                OAuthClient = PublicClientApplicationBuilder
                    .Create(ClientId)
                    .WithDefaultRedirectUri()
                    .WithParentActivityOrWindow(Window.GetForegroundWindow)
                    .WithBroker(options)
                    .Build();
            }
        }
        public static Tuple<bool,AuthenticationResult?> MSLoginWithWAM() {
            try {
                Logger.Log("[Account] Microsoft ��¼��ʼ����Ȩ��������¼��");
                if (ClientId != OriginId) OAuthClient = null; OriginId = ClientId;
                if (OAuthClient is null ) InitOAuthClient();
                Logger.Log("[Account] ��ʼ�� WAM �ɹ�");
                AuthenticationResult? Result = OAuthClient
                    .AcquireTokenInteractive(Scope)
                                .ExecuteAsync()
                                .GetAwaiter()
                                .GetResult();
                Logger.Log("[Account] Microsoft ��¼�ɹ�");
                Logger.Log($"[Account] ���ƹ���ʱ�䣺{Result.ExpiresOn}");
                
                return Tuple.Create<bool, AuthenticationResult?>(true,Result);
            } catch (MsalUiRequiredException ex) {
                Logger.Log(ex, "[Account] Microsoft ��¼ʧ��");
                return Tuple.Create<bool, AuthenticationResult?>(false, null);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Microsoft ��¼ʧ��");
                throw;
            }
        }
        internal async static Task MSLoginDeviceCallback(DeviceCodeResult Result)
        {
            UserCode = Result.UserCode;
            VerificationUrl = Result.VerificationUrl;
        }
        internal static Tuple<bool,AuthenticationResult?> MSLoginDevice()
        {
            Logger.Log("[Account] Microsoft ��¼��ʼ���豸��������¼��");
            try
            {
                var Result = OAuthClient
                    .AcquireTokenWithDeviceCode(Scope, MSLoginDeviceCallback)
                    .ExecuteAsync()
                    .GetAwaiter()
                    .GetResult();
                return Tuple.Create<bool,AuthenticationResult?>(true, Result);
            }catch(MsalException ex){
                Logger.Log(ex, "[Account] Microsoft ��¼ʧ��");
                return Tuple.Create<bool, AuthenticationResult?>(false, null);
            }catch(Exception ex)
            {
                Logger.Log(ex, "[Account] Microsoft ��¼�����г���δ֪����");
                throw;
            }
        } 
        internal static AuthenticationResult? MSLoginRefresh()
        {
            return null;
        }
        internal async static Task<IEnumerable<IAccount>> GetLoginAccount()
        {
            if (OAuthClient is null) InitOAuthClient();
            return await OAuthClient.GetAccountsAsync();
        }
        public static int MSALogin(bool PerferDeviceLogin)
        {
            Tuple<bool,AuthenticationResult?>? Result;
            if (PerferDeviceLogin) Result = MSLoginDevice();
            else Result = MSLoginWithWAM();
                return 0;
        }
    }
}