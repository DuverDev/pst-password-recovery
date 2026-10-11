using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace PST_PasswordRecovery
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbeddedAssembly;
            base.OnStartup(e);
        }

        private static Assembly ResolveEmbeddedAssembly(object sender, ResolveEventArgs args)
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            string targetAssemblyName = new AssemblyName(args.Name).Name + ".dll";
            string resourceName = "EmbeddedDependencies." + targetAssemblyName;

            using (Stream stream = currentAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                byte[] assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return Assembly.Load(assemblyData);
            }
        }
    }
}
