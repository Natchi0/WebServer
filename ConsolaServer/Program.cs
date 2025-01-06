using System;
using System.Reflection;
using WebServer;

Server.Start();
Console.ReadLine();

static string GetWebsitePath()
{
    //path del exe de la consola
    string websitePath = Assembly.GetExecutingAssembly().Location;
    return websitePath;
}