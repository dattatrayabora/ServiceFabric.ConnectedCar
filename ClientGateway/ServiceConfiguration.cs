
namespace ClientGateway
{
    using System.Fabric;
    using System.Fabric.Description;

    class ServiceConfiguration
    {
        private static ServiceInitializationParameters serviceInitializationParameters;
        private static ConfigurationSection configurationSection;
        public static void InitializeServiceParameters(ServiceInitializationParameters initparams)
        {
            serviceInitializationParameters = initparams;
            var configurationPackage = serviceInitializationParameters.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            configurationSection = configurationPackage.Settings.Sections["ConfigurationSettings"];
        }

        public static string GetConfiguration(string name)
        {
            if(configurationSection.Parameters.Contains(name))
            {
                return configurationSection.Parameters[name].Value;
            }

            return null;
        }
    }
}
