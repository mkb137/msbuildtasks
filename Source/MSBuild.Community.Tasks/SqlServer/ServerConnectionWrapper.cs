// $Id$
using System;
using System.Data.SqlClient;
using System.Reflection;

namespace MSBuild.Community.Tasks.SqlServer
{
	internal class ServerConnectionWrapper
	{
		public static string AssemblyName = "Microsoft.SqlServer.ConnectionInfo, Version=9.0.242.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91, processorArchitecture=MSIL";
		private static volatile Assembly _assembly;
		private static object _lock = new object();
		private Type _type;
		private object _instance;
		
		public ServerConnectionWrapper()
		{
			LoadAssembly();
			_type = _assembly.GetType("Microsoft.SqlServer.Management.Common.ServerConnection", true);
			_instance = _assembly.CreateInstance(_type.FullName);
			if (_instance == null)
			{
				throw new ArgumentNullException("Could not create type " + _type.FullName, (Exception)null);
			}
		}
		
		private static void LoadAssembly()
		{
			if (_assembly == null)
			{
				lock(_lock)
				{
					if (_assembly == null)
					{
						_assembly = Assembly.Load(AssemblyName);
					}
				}
			}
			if (_assembly == null)
			{
				throw new ArgumentNullException("Could not load assembly {0}", AssemblyName);
			}
		}
		
		public string ConnectionString
		{
			get { return (string)_type.GetProperty("ConnectionString").GetValue(_instance, null); }
			set { _type.GetProperty("ConnectionString").SetValue(_instance, value, null); }
		}
		
		public string BatchSeparator
		{
			get { return (string)_type.GetProperty("BatchSeparator").GetValue(_instance, null); }
			set { _type.GetProperty("BatchSeparator").SetValue(_instance, value, null); }
		}

		public int StatementTimeout
		{
			get { return (int)_type.GetProperty("StatementTimeout").GetValue(_instance, null); }
			set { _type.GetProperty("StatementTimeout").SetValue(_instance, value, null); }
		}
		
		public event SqlInfoMessageEventHandler InfoMessage
		{
			add
			{
				EventInfo e = _type.GetEvent("InfoMessage");
				e.AddEventHandler(_instance, value);
			}
			remove
			{
				EventInfo e = _type.GetEvent("InfoMessage");
				e.RemoveEventHandler(_instance, value);
			}
		}
		
		public void Connect()
		{
			InternalInvoke<object>("Connect", null);	
		}
		
		public void Disconnect()
		{
			InternalInvoke<object>("Disconnect", null);
		}
		
		public int ExecuteNonQuery(string command)
		{
			return InternalInvoke<int>("ExecuteNonQuery", new object[] { command } );
		}

		private T InternalInvoke<T>(string name, object[] args)
		{
			object result = null;
			try
			{
				result = _type.InvokeMember(name, BindingFlags.Instance | BindingFlags.Public |
					BindingFlags.InvokeMethod, null, _instance, args);
			}
			catch (TargetInvocationException e)
			{
				HandleReflectionException(e);
			}
			return (T)result;
		}

		private static object CreateInstance(Type type, params object[] args)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | 
				BindingFlags.CreateInstance | BindingFlags.Instance;
			object instance = null;
			try
			{
				instance = Activator.CreateInstance(type, flags, null, args, null, null);
			}
			catch (TargetInvocationException e)
			{
				HandleReflectionException(e);
			}
			return instance;
		}
		
		private static void HandleReflectionException(TargetInvocationException e)
		{
			Exception inner = e.InnerException;
			if (inner != null)
			{
				throw (Exception)CreateInstance(e.InnerException.GetType(),
					e.InnerException.Message, e.InnerException);
			}
			else
			{
				throw (Exception)CreateInstance(e.GetType(), e.Message, e);
			}
		}
	}
}
