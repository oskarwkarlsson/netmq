﻿using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace NetMQ.zmq
{
	public class MonitorEvent
	{
		private const int ValueInteger = 1;
		private const int ValueChannel = 2;

		private readonly SocketEvent m_monitorEvent;
		private readonly String m_addr;
		private readonly Object m_arg;
		private readonly int m_flag;

		private static readonly int SizeOfIntPtr;

		static MonitorEvent()
		{
			SizeOfIntPtr = Marshal.SizeOf(typeof(IntPtr));

			if (SizeOfIntPtr > 4)
			{
				SizeOfIntPtr = 8;
			}
		}

		public MonitorEvent(SocketEvent monitorEvent, String addr, Object arg)
		{
			this.m_monitorEvent = monitorEvent;
			this.m_addr = addr;
			this.m_arg = arg;
			if (arg is int)
				m_flag = ValueInteger;
			else if (arg is System.Net.Sockets.Socket)
				m_flag = ValueChannel;
			else
				m_flag = 0;
		}

		public string Addr
		{
			get { return m_addr; }
		}

		public object Arg
		{
			get { return m_arg; }
		}

		public int Flag
		{
			get { return m_flag; }
		}

		public SocketEvent Event
		{
			get { return m_monitorEvent; }
		}

		public bool Write(SocketBase s)
		{
			int size = 4 + 1 + m_addr.Length + 1; // event + len(addr) + addr + flag
			if (m_flag == ValueInteger)
			  size += 4;
			else if (m_flag == ValueChannel)
			{
				size += SizeOfIntPtr;
			}		
			
			int pos = 0;

			ByteArraySegment buffer = new byte[size];
			buffer.PutInteger((int)m_monitorEvent, pos);
			pos += 4;
			buffer[pos++] = (byte)m_addr.Length;

			// was not here originally

			buffer.PutString(m_addr, pos);
			pos += m_addr.Length;

			buffer[pos++] = ((byte)m_flag);
			if (m_flag == ValueInteger)
			{
				buffer.PutInteger((int) m_arg, pos);
			}
			else if (m_flag == ValueChannel)
			{
				if (SizeOfIntPtr == 4)
				{
					buffer.PutInteger(((Socket)m_arg).Handle.ToInt32(), pos);
				}
				else
				{
					buffer.PutLong(((Socket)m_arg).Handle.ToInt64(), pos);
				}
			}			

			Msg msg = new Msg((byte[])buffer);
			return s.Send(msg, 0);
		}

		public static MonitorEvent Read(SocketBase s)
		{
			Msg msg = s.Recv(0);
			if (msg == null)
				return null;

			int pos = 0;
			ByteArraySegment data = msg.Data;

			SocketEvent @event =(SocketEvent) data.GetInteger(pos);				
			pos += 4;
			int len = (int)data[pos++];
			string addr = data.GetString(len, pos);
			pos += len;
			int flag = (int)data[pos++];
			Object arg = null;

			if (flag == ValueInteger)
			{
				arg = data.GetInteger(pos);
			}
			else if (flag == ValueChannel)
			{
				if (SizeOfIntPtr == 4)
				{
					arg = new IntPtr(data.GetInteger(pos));
				}
				else
				{
					arg = new IntPtr(data.GetLong(pos));
				}
			}			


			return new MonitorEvent(@event, addr, arg);
		}
	}
}