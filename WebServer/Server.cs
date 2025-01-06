using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WebServer
{
	public static class Server
	{
		private static HttpListener Listener;
		private static int maxSimultaneousConnections = 10;
		//genero semaforo para controlar la cantidad de conexiones simultaneas, empezando en 10 y con un maximo de 10
		private static Semaphore sem = new Semaphore(maxSimultaneousConnections, maxSimultaneousConnections);
		private static Router router = new Router();

        public static void Start()
		{
			List<IPAddress> localHostIps = GetLocalHostIPs();
			HttpListener listener = InitializeListener(localHostIps);
			Start(listener);
		}

		/// <summary>
		/// Manda a inicializar el listener en un hilo aparte mediante Task.Run
		/// </summary>
		private static void Start(HttpListener listener, string websitePath)
		{
			router.websitePath = websitePath;
			listener.Start();
			Task.Run(() => RunServer(listener));
		}

		public static void Log(HttpListenerRequest request)
		{
			Console.WriteLine($"Request: {request.HttpMethod} / {request.Url}");
		}

		private static Dictionary<string, string> GetKeyValues(string parms)
		{
			if (string.IsNullOrEmpty(parms))
				return new Dictionary<string, string>();

			Dictionary<string, string> ret = new Dictionary<string, string>();
			var kv = parms.Split('&').ToList();

            for (int i = 0; i < kv.Count; i++)
            {
                ret.Add(kv[i].Split('=')[0], kv[i].Split('=')[1]);
            }
			return ret;
        }

		/// <summary>
		/// Inicializa el servidor web en un hilo aparte
		/// </summary>
		private static void RunServer(HttpListener listener)
		{
			while (true)
			{ 
				sem.WaitOne(); //espera que el semaforo tenga un lugar libre
				StartConnectionListener(listener);
			}
		}

		/// <summary>
		/// Espera una conexión y la procesa
		/// </summary>
		private static async void StartConnectionListener(HttpListener listener)
		{
			// se espera por una conexion, mientras tanto se retorna al caller
			HttpListenerContext context = await listener.GetContextAsync();

			// se libera el semaforo para que otro hilo pueda tomar su lugar
			sem.Release();
			Log(context.Request);

			/*
			 * ESTE CODIGO ES LA VERSION VIEJA, IGNORAR
			 * 
				// se procesa la conexion
				string response = "<html><head><meta http-equiv='content-type' content='text/html; charset=utf-8'/> </head> Hello Browser! </html>"; //mensaje de respuesta
				byte[] encoded = Encoding.UTF8.GetBytes(response);//codifico el mensaje en bytes
				context.Response.ContentLength64 = encoded.Length;//seteo el larfo del mensaje para el cliente
				context.Response.OutputStream.Write(encoded, 0, encoded.Length);//lo escribo en la respuesta
				context.Response.OutputStream.Close();//cierro la respuesta
			*/

			// se procesa la conexion
			HttpListenerRequest request = context.Request;
			string path = request.RawUrl.LeftOf("?"); //obtengo la url sin los parametros
			string verb = request.HttpMethod; //obtengo el verbo http
			string parms = request.RawUrl.RightOf("?"); //obtengo los parametros
			Dictionary<string, string> kvParams = GetKeyValues(parms); //obtengo los parametros en un diccionario

			router.Route(verb, path, kvParams);
		}

		/// <summary>
		/// Obtiene la lista de direcciones IP de la máquina local
		/// </summary>
		private static List<IPAddress> GetLocalHostIPs()
		{
			// Obtengo los datos del host local
			IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

			// Filtro para obtener las ip en IPv4 (InterNetwork)
			List<IPAddress> ret = host.AddressList
				.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
				.ToList();

			return ret;
		}

		/// <summary>
		/// Inicializar el coso que escucha a http tanto para localhost
		/// como para las otras IPs de la maquina
		/// </summary>
		private static HttpListener InitializeListener(List<IPAddress> localhostIps)
		{
			// creo el listener y le agrego el prefijo para localhost
			HttpListener listener = new HttpListener();
			listener.Prefixes.Add("http://localhost/");

			// agrego los prefijos para las otras IPs
			foreach (IPAddress ip in localhostIps)
			{
				Console.WriteLine($"Escuchando en http://{ip.ToString()}/");
				listener.Prefixes.Add($"http://{ip.ToString()}/");
			}

			return listener;
		}


	}

	public static class StringExtensions
	{
		public static string LeftOf(this string source, string value)
		{
			if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
			{
				return source;
			}

			int index = source.IndexOf(value);
			return index == -1 ? source : source.Substring(0, index);
		}

		public static string RightOf(this string source, string value)
		{
			if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
			{
				return source;
			}
			int index = source.IndexOf(value);
			return index == -1 ? source : source.Substring(index + source.Length);
		}
	}
}
