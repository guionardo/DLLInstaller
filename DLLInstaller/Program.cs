using System;
using System.Diagnostics;
using System.IO;

namespace DLLInstaller
{
    class Program
    {
        private static string arquivoDLL;
        private static string regasm;

        private static string GetRegAsm()
        {
            var _regasm = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET");
            if (Directory.Exists(Path.Combine(_regasm, "Framework64")))
                _regasm = Path.Combine(_regasm, "Framework64");
            else if (Directory.Exists(Path.Combine(_regasm, "Framework")))
                _regasm = Path.Combine(_regasm, "Framework");
            else
            {
                Mensagem("Framework .net não foi encontrado!");
                return "";
            }

            var frameworks = Directory.GetDirectories(_regasm, "v*");
            if (frameworks.Length == 0) return "";

            _regasm = Path.Combine(frameworks[frameworks.Length - 1], "regasm.exe");
            if (!File.Exists(_regasm))
            {
                Mensagem("Arquivo regasm.exe não foi encontrado!");
                return "";
            }

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(_regasm);
            Mensagem(_regasm + " " + fvi.FileVersion);

            return _regasm;
        }

        static int Main(string[] args)
        {
            Mensagem("Instalador de DLL .net");
            if (args.Length == 0)
            {
                Mensagem("Uso: dllinstaller.exe <arquivo.dll>");
                return 1;
            }

            arquivoDLL = args[0];
            if (!File.Exists(arquivoDLL))
            {
                Mensagem("Arquivo " + arquivoDLL + " não encontrado");
                return 1;
            }

            arquivoDLL = Path.GetFullPath(arquivoDLL);
            if (!Path.GetExtension(arquivoDLL).Equals(".dll", StringComparison.InvariantCultureIgnoreCase))
            {
                Mensagem("Arquivo " + arquivoDLL + " inválido");
                return 1;
            }

            regasm = GetRegAsm();

            if (string.IsNullOrEmpty(regasm))
            {
                Mensagem("Arquivo regasm.exe não encontado");
                return 1;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = regasm,
                Arguments = $"\"{arquivoDLL}\" /s /codebase",
                UseShellExecute = true,
                Verb = "runas"
            };


            int retorno = 0;
            try
            {
                Process p = Process.Start(startInfo);
                p.WaitForExit();

                retorno = p.ExitCode;
                Console.WriteLine($"ExitCode={retorno}");
                p.Close();
                return retorno;
            }
            catch (Exception e)
            {
                Mensagem("Erro na execução do comando: " + e.Message);
                return 1;
            }
        }

        static void Mensagem(string mensagem) =>
                            Console.WriteLine(mensagem);
    }
}
