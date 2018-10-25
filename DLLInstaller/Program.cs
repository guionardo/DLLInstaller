using DLLInstaller.Auxiliar;
using System;
using System.Diagnostics;
using System.IO;

namespace DLLInstaller
{
    class Program
    {
        private static string arquivoDLL;
        static string pathApp = "";
        private static string regasm;
        private static bool unregister = false;

        /// <summary>
        /// Retorna o caminho do executável regasm.exe do Framework .net
        /// </summary>
        /// <returns></returns>
        static string GetRegAsm()
        {
            var _regasm = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET");
            if (Directory.Exists(Path.Combine(_regasm, "Framework64")))
                _regasm = Path.Combine(_regasm, "Framework64");
            else if (Directory.Exists(Path.Combine(_regasm, "Framework")))
                _regasm = Path.Combine(_regasm, "Framework");
            else
            {
                Dialogs.Error("Framework .net não foi encontrado!", true);
                return "";
            }

            var frameworks = Directory.GetDirectories(_regasm, "v*");
            if (frameworks.Length == 0) return "";

            _regasm = Path.Combine(frameworks[frameworks.Length - 1], "regasm.exe");
            if (!File.Exists(_regasm))
            {
                Dialogs.Error("Arquivo regasm.exe não foi encontrado!", true);
                return "";
            }

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(_regasm);
            Log.Add("REGASM = " + _regasm + " " + fvi.FileVersion);

            return _regasm;
        }

        static int Main(string[] args)
        {
            Mensagem("Instalador de DLL .net");


            string sArgs = "";
            foreach (var a in args) sArgs += a + " ";
            Log.Add("DLLInstaller " + sArgs);
            if (!ParseArgs(args, out string msg))
            {
                Log.Add(msg);
                Mensagem($"{msg}" +
                    $"\n\nUso: dllinstaller.exe <arquivo.dll> [-u]" +
                    $"\n\nA dll registrada será copiada para o diretório {pathApp}");
                return 1;
            }

            var r = RunRegAsm();
            Log.Add("SUCESSO = " + (r == 1 ? "SIM" : "NÃO"));
            Log.Add("*");
            return r;
        }

        static void Mensagem(string mensagem) =>
                            Console.WriteLine(mensagem);

        private static bool ParseArgs(string[] args, out string msg)
        {
            if (args.Length == 0)
            {
                msg = "Parâmetros não foram informados";
                return false;
            }

            foreach (var a in args)
                if (a.Equals("-u", StringComparison.InvariantCultureIgnoreCase) ||
                    a.Equals("/u", StringComparison.InvariantCultureIgnoreCase))
                    unregister = true;
                else
                    arquivoDLL = a;

            if (!File.Exists(arquivoDLL))
            {
                msg = "Arquivo " + arquivoDLL + " inexistente";
                return false;
            }

            if (!Path.GetExtension(arquivoDLL).Equals(".dll", StringComparison.InvariantCultureIgnoreCase))
            {
                msg = "Arquivo " + arquivoDLL + " não é uma DLL";
                return false;
            }

            regasm = GetRegAsm();

            if (string.IsNullOrEmpty(regasm))
            {
                msg = "Arquivo regasm.exe não encontado";
                return false;
            }


            pathApp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DLLInstaller");
            if (!Directory.Exists(pathApp))
            {
                try
                {
                    Directory.CreateDirectory(pathApp);
                    Log.Add("CREATE " + pathApp);
                }
                catch (Exception e)
                {
                    Log.Add("ERROR " + e.Message);
                    Dialogs.Error("Ocorreu um erro ao criar a pasta " + pathApp + "\n\n" + e.Message +
                        "\n\nSerá utilizado o diretório corrente: " + Directory.GetCurrentDirectory());
                    pathApp = Directory.GetCurrentDirectory();
                    msg = e.Message;
                }
                Log.Add("PATHAPP = " + pathApp);
            }
            msg = "OK";
            return true;
        }


        static int RunRegAsm()
        {
            string winsys = Path.Combine(pathApp, Path.GetFileName(arquivoDLL));
            string outmsg = Path.Combine(pathApp,
                "dllinstaller." + Path.GetFileNameWithoutExtension(arquivoDLL) + "." +
                DateTime.Now.ToString("yyyyMMddHHmmss") + ".out");
            string batchfile = Path.ChangeExtension(outmsg, ".cmd");
            Log.Add("DLL    = " + winsys);
            Log.Add("OUTMSG = " + outmsg);
            Log.Add("BATCH  = " + batchfile);
            string batch = @"@echo off
if exist #outmsg del #outmsg
if exist #winsys goto jaexiste
echo Copiando #arquivoDLL para #winsys >> #outmsg
copy /Y #arquivoDLL #winsys >> #outmsg
if errorlevel 1 goto errocopia
:jaexiste
if not exist #winsys goto errocopia
echo Registrando #winsys
#regasm #winsys /s /codebase /verbose #unregister >> #outmsg
if errorlevel 100 goto erroregasm
echo #winsys REGISTRADO #unregister >> #outmsg
echo 1 >> #outmsg
goto fim
:errocopia
echo ERRO AO COPIAR >> #outmsg
echo 0 >> #outmsg
goto fim
:erroregasm
echo ERRO AO REGISTRAR #winsys >> #outmsg
echo 0 >> #outmsg
:fim";
            batch = batch.
                Replace("#outmsg", "\"" + outmsg + "\"").
                Replace("#arquivoDLL", "\"" + arquivoDLL + "\"").
                Replace("#winsys", "\"" + winsys + "\"").
                Replace("#unregister", unregister ? "/u" : "").
                Replace("#regasm", "\"" + regasm + "\"");

            try
            {
                File.WriteAllText(batchfile, batch);
            }
            catch (Exception e)
            {
                Mensagem("Erro " + e.Message);
                return 1;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c " + batchfile,
                UseShellExecute = true,
                Verb = "runas"
            };

            int retorno = 0;
            try
            {
                Log.Add("BATCH\n" + batch);

                Log.Add("Process START");
                Process p = Process.Start(startInfo);
                p.WaitForExit();

                retorno = p.ExitCode;
                Log.Add("Process END - ExitCode=" + retorno);

                p.Close();
                try { File.Delete(batchfile); } catch { }

                if (File.Exists(outmsg))
                {
                    var s = File.ReadAllText(outmsg);

                    if (s.Length > 0)
                    {
                        var l = s.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        Log.Add("OUTMSG\n" + s);
                        int.TryParse(l[l.Length - 1], out retorno);

                    }

                }
                return retorno;
            }
            catch (Exception e)
            {
                Mensagem("Erro na execução do comando: " + e.Message);
                return 1;
            }
        }
    }
}
