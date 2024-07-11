namespace Saraki_Client
{
    using System;
    //Me permite usar la interoperabilidad para poder acceder a DLL no .NET
    using System.Runtime.InteropServices;
    
    public class Functions_Externs
    {
        //Abre y Cierra la Torre de CD
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA")]
        public static extern Int32 mciSendStringA(string command, string back, 
        long longitud, long callbacknow);
    }
}
