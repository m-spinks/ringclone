using NHibernate;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Helpers
{
	public class EncryptedString : IUserType
	{

		public object Assemble(object cached, object owner)
		{
			return cached;
		}

		public object DeepCopy(object value)
		{
			if (value == null) return null;
			else return new string(value.ToString().ToCharArray());
		}

		public object Disassemble(object value)
		{
			return value;
		}

		public new bool Equals(object x, object y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (x == null || y == null)
			{
				return false;
			}
			return x.Equals(y);
		}

		public int GetHashCode(object x)
		{
			return x.GetHashCode();
		}

		public bool IsMutable
		{
			get { return false; }
		}

		public object NullSafeGet(IDataReader rs, string[] names, object owner)
		{
			string strString = (string)NHibernate.NHibernateUtil.String.NullSafeGet(rs, names[0]);
			string result = new string(DatabaseDecrypt(strString).ToCharArray());
			return result;
		}

		public void NullSafeSet(IDbCommand cmd, object value, int index)
		{
			if (value == null)
			{
				NHibernateUtil.String.NullSafeSet(cmd, null, index);
				return;
			}
			else
			{
				value = DatabaseEncrypt((string)value);
				NHibernate.NHibernateUtil.String.NullSafeSet(cmd, value, index);
			}
		}

		public object Replace(object original, object target, object owner)
		{
			return original;
		}

		public Type ReturnedType
		{
			get { return typeof(string); }
		}

		public SqlType[] SqlTypes
		{
			get { return new[] { NHibernateUtil.String.SqlType }; }
		}



		public static string DatabaseEncrypt(string str)
		{
			string strE = "";
			try
			{
				strE = RijndaelSimple.Encrypt(str, EncryptionSettings.PassPhrase, EncryptionSettings.Salt, EncryptionSettings.HashAlgorithm, EncryptionSettings.Iterations, EncryptionSettings.InitVector, EncryptionSettings.KeySize);
			}
			catch (Exception ex)
			{

			}
			return strE;
		}

		public static string DatabaseDecrypt(string str)
		{
			string strD = "";
			try
			{
				strD = RijndaelSimple.Decrypt(str, EncryptionSettings.PassPhrase, EncryptionSettings.Salt, EncryptionSettings.HashAlgorithm, EncryptionSettings.Iterations, EncryptionSettings.InitVector, EncryptionSettings.KeySize);
			}
			catch (Exception ex)
			{

			}
			return strD;
		}

		public static class EncryptionSettings
		{
			public static string PassPhrase
			{
				get { return "passphrase"; }
			}
			public static string Salt
			{
				get { return "salt"; }
			}
			public static string HashAlgorithm
			{
				get { return "SHA1"; }
			}
			public static int KeySize
			{
				get { return 256; }
			}
			public static string InitVector
			{
				get { return "G50KE@i*^nH!l0Ps"; }
			}
			public static int Iterations
			{
				get { return 2; }
			}
		}

	}

}