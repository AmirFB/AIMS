using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DevExpress.Xpf.Core;
using static AIMS3.BackEnd.Cryptography.AES;

namespace AIMS3.BackEnd.Cryptography
{
	public static class AES
	{
		public enum AESType { AES128 = 16, AES192 = 24, AES256 = 32 };

		public static byte[] Encrypt(string plainText, byte[] Key, byte[] IV)
		{
			byte[] encrypted;

			using (AesManaged aes = new AesManaged())
			{
				ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);

				using (MemoryStream ms = new MemoryStream())
				{
					using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
					{
						using (StreamWriter sw = new StreamWriter(cs))
							sw.Write(plainText);

						encrypted = ms.ToArray();
					}
				}
			}

			return encrypted;
		}

		public static byte[] Encrypt(byte[] plainData, byte[] Key, byte[] IV)
		{
			byte[] encrypted;

			using (AesManaged aes = new AesManaged())
			{
				ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);

				using (MemoryStream ms = new MemoryStream())
				{
					using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
					{
						using (StreamWriter sw = new StreamWriter(cs))
							sw.Write(Encoding.UTF8.GetString(plainData));

						encrypted = ms.ToArray();
					}
				}
			}

			return encrypted;
		}

		public static string Decrypt(byte[] cipherText, byte[] Key, byte[] IV)
		{
			string plaintext = null;

			try
			{
				using (AesManaged aes = new AesManaged())
				{
					ICryptoTransform decryptor = aes.CreateDecryptor(Key, IV);

					using (MemoryStream ms = new MemoryStream(cipherText))
					{
						using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
						{
							using (StreamReader reader = new StreamReader(cs))
								plaintext = reader.ReadToEnd();
						}
					}
				}
			}
			catch (Exception ex) { }
			return plaintext;
		}

		public static byte[] DefaultKey(int seed)
		{
			byte[] output = new byte[32];
			AIRandom random = new AIRandom(seed);

			for (int i = 0; i < 32; i++)
				output[i] = (byte)random.Next();

			return output;
		}

		public static byte[] DefaultIV(int seed)
		{
			byte[] output = new byte[16];
			AIRandom random = new AIRandom(seed);
			// Random random = new Random(79);

			for (int i = 0; i < 16; i++)
				output[i] = (byte)random.Next();

			return output;
		}
	}

	public static class AES2
	{
		public static byte[] Encrypt(string plainText, byte[] Key, byte[] IV)
		{
			byte[] encrypted;

			try
			{
				using (AesManaged aes = new AesManaged())
				{
					ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);

					using (MemoryStream ms = new MemoryStream())
					{
						using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
						{
							using (StreamWriter sw = new StreamWriter(cs))
								sw.Write(AIB64.Encode(plainText));

							encrypted = ms.ToArray();
						}
					}
				}

				return encrypted;
			}
			catch (Exception ex) { }
			return null;
		}

		public static byte[] Encrypt(byte[] plainData, byte[] Key, byte[] IV)
		{
			byte[] encrypted;

			using (AesManaged aes = new AesManaged())
			{
				ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);

				using (MemoryStream ms = new MemoryStream())
				{
					using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
					{
						using (StreamWriter sw = new StreamWriter(cs))
							sw.Write(AIB64.Encode(Encoding.UTF8.GetString(plainData)));

						encrypted = ms.ToArray();
					}
				}
			}

			return encrypted;
		}

		public static string Decrypt(byte[] cipherText, byte[] Key, byte[] IV)
		{
			string plaintext = null;

			try
			{
				using (AesManaged aes = new AesManaged())
				{
					ICryptoTransform decryptor = aes.CreateDecryptor(Key, IV);

					using (MemoryStream ms = new MemoryStream(cipherText))
					{
						using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
						{
							using (StreamReader reader = new StreamReader(cs))
								plaintext = reader.ReadToEnd();
						}
					}
				}
			}

			catch { }
			try
			{
				return AIB64.Decode(plaintext);
			}
			catch (Exception ex) { }
			return "";
			
		}

		public static byte[] DefaultKey(int seed, AESType type)
		{
			byte[] output = new byte[(int)type];
			AIRandom random = new AIRandom(seed);

			for (int i = 0; i < output.Length; i++)
				output[i] = (byte)random.Next();

			return output;
		}

		public static byte[] DefaultIV(int seed)
		{
			byte[] output = new byte[16];
			AIRandom random = new AIRandom(seed);
			// Random random = new Random(79);

			for (int i = 0; i < 16; i++)
				output[i] = (byte)random.Next();

			return output;
		}
	}

	public static class AIB64
	{
		static Random random = new Random();

		public static string Encode(string input)
		{
			string output = "", dummy = "";
			int temp = 0;
			byte[] data = Encoding.ASCII.GetBytes(input);
			byte[] dummyBytes;

			for (int i = 0; i < (data.Length + 2) / 3; i++, dummy = "")
			{
				if (i != (data.Length - 1) / 3 || data.Length % 3 == 0)
				{
					temp = (data[i * 3] << 16) | (data[i * 3 + 1] << 8) | data[i * 3 + 2];

					for (int j = 0; j < 4; j++, temp >>= 6)
						dummy += Encode((byte)(temp & 63));
				}

				else if (data.Length % 3 == 2)
				{
					temp = (data[i * 3] << 10) | (data[i * 3 + 1] << 2);

					for (int j = 0; j < 3; j++, temp >>= 6)
						dummy += Encode((byte)(temp & 0X3F));
				}

				else if (data.Length % 3 == 1)
				{
					temp = (data[i * 3] << 4);

					for (int j = 0; j < 2; j++, temp >>= 6)
						dummy += Encode((byte)(temp & 0X3F));
				}

				dummyBytes = Encoding.ASCII.GetBytes(dummy);
				System.Array.Reverse(dummyBytes);
				output += Encoding.ASCII.GetString(dummyBytes);
			}

			return output;
		}

		public static string Decode(string input)
		{
			string output = "", dummy = "";
			int temp = 0;
			byte[] data = Encoding.ASCII.GetBytes(input);
			byte[] dummyBytes;

			for (int i = 0; i < (data.Length + 3) / 4; i++, dummy = "")
			{
				if (i != (data.Length - 1) / 4 || data.Length % 4 == 0)
				{
					temp = (Decode(data[i * 4]) << 18) | (Decode(data[i * 4 + 1]) << 12) | (Decode(data[i * 4 + 2]) << 6) | Decode(data[i * 4 + 3]);

					for (int j = 0; j < 3; j++, temp >>= 8)
						dummy += Convert.ToChar(temp & 255);
				}

				else if (data.Length % 4 == 3)
				{
					temp = (Decode(data[i * 4]) << 10) | (Decode(data[i * 4 + 1]) << 4) | (Decode(data[i * 4 + 2]) >> 2);

					for (int j = 0; j < 2; j++, temp >>= 8)
						dummy += Convert.ToChar(temp & 255);
				}

				else if (data.Length % 4 == 2)
				{
					temp = (Decode(data[i * 4]) << 2) | (Decode(data[i * 4 + 1]) >> 4);

					for (int j = 0; j < 1; j++, temp >>= 8)
						dummy += Convert.ToChar(temp & 255);
				}

				dummyBytes = Encoding.ASCII.GetBytes(dummy);
				System.Array.Reverse(dummyBytes);
				output += Encoding.ASCII.GetString(dummyBytes);
			}

			return output;
		}

		private static char Encode(byte input)
		{
			if (input >= 0 && input < 26)
				return Convert.ToChar(input + 97 + ((input % 2 == 0) ? 1 : -1));

			if (input == 26)
				return random.NextDouble() >= 0.5 ? '-' : '_';

			if (input == 27)
				return random.NextDouble() >= 0.5 ? '!' : '?';

			if (input >= 28 && input <= 37)
				return Convert.ToChar(input + 47 - 27);

			if (input >= 38 && input < 64)
				return Convert.ToChar(input + 64 - 37 + ((input % 2 == 0) ? 1 : -1));

			return Convert.ToChar(0);
		}

		private static byte Decode(byte input)
		{
			if (input > 96 && input <= 122)
				return (byte)(input - 97 + ((input % 2 == 0) ? -1 : 1));

			if (input == (byte)'-' || input == (byte)'_')
				return 26;

			if (input == (byte)'!' || input == (byte)'?')
				return 27;

			if (input >= 48 && input < 58)
				return (byte)(input - 47 + 27);

			if (input > 64 && input <= 90)
				return (byte)(input - 64 + 37 + ((input % 2 == 0) ? -1 : 1));

			return 64;
		}
	}

	public static class SHA
	{
		public static string Hash(string input)
		{
			using (SHA512 shaM = new SHA512Managed())
			{
				return Encoding.ASCII.GetString(shaM.ComputeHash(Encoding.ASCII.GetBytes(input)));
			}
		}
	}

	public static class AIES
	{
		public static byte[] Encrypt(string input, AESType type)
		{
			RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
			byte[] key = new byte[(int)type];
			byte[] iv = new byte[16];
			List<byte> aes = new List<byte>();
			byte[] cipher;
			List<byte> output = new List<byte>();

			random.GetBytes(key);
			random.GetBytes(iv);

			cipher = AES.Encrypt(Encoding.ASCII.GetBytes(input), key, iv);
			aes.AddRange(key);
			aes.AddRange(iv);
			aes = Complement(aes.ToList());

			for (int i = 0; i < Math.Min(cipher.Length, aes.Count); i++)
			{
				output.Add(cipher[i]);
				output.Add(aes[i]);
			}

			if (cipher.Length > aes.Count)
				for (int i = Math.Min(cipher.Length, aes.Count); i < cipher.Length; i++)
					output.Add(cipher[i]);

			else
				for (int i = Math.Min(cipher.Length, aes.Count); i < aes.Count; i++)
					output.Add(aes[i]);

			return output.ToArray();
		}

		public static string Decrypt(byte[] input, AESType type)
		{
			byte[] key = new byte[(int)type];
			byte[] iv = new byte[16];
			List<byte> aes = new List<byte>();
			List<byte> cipher = new List<byte>();
			int count = 16 + (int)type;
			int min = Math.Min((input.Length - count) * 2, count * 2);
			int index = 0;

			while (index < min)
			{
				cipher.Add(input[index++]);
				aes.Add(input[index++]);
			}

			if (input.Length > count * 2)
				while (index < input.Length)
					cipher.Add(input[index++]);

			else
				while (index < input.Length)
					aes.Add(input[index++]);

			aes = Complement(aes);
			key = aes.GetRange(0, (int)type).ToArray();
			iv = aes.GetRange((int)type, 16).ToArray();

			return AES.Decrypt(cipher.ToArray(), key, iv);
		}

		public static List<byte> Complement(List<byte> data)
		{
			data.ForEach(b => b = (byte)(256 - b));
			return data;
		}
	}

	public class AIRandom
	{
		private int value = 5, valueo = 101, a = 287, b = 59, c = 117, m = 256, min = 0;

		public AIRandom()
		{ }

		public AIRandom(int seed)
		{
			value = seed;
		}

		public AIRandom(int min, int max)
		{
			this.min = min;
			m = max - min;

			if (a > m)
			{
				a = (int)(a * 256 / m);
				a += a % 2 + 1;
			}

			if (b > m)
			{
				b = (int)(b * 256 / m);
				b += b % 2 + 1;
			}

			if (c > m)
			{
				c = (int)(c * 256 / m);
				c += c % 2;
			}
		}

		public AIRandom(int seed, int min, int max)
		{
			value = seed;
			this.min = min;
			m = max - min;

			if (a > m)
			{
				a = (int)(a * 256 / m);
				a += a % 2 + 1;
			}

			if (b > m)
			{
				b = (int)(b * 256 / m);
				b += b % 2 + 1;
			}

			if (c > m)
			{
				c = (int)(c * 256 / m);
				c += c % 2;
			}
		}

		public int Next()
		{
			int temp = value;
			value = (a * value + b * valueo + c) % m;
			valueo = temp;

			return min + value;
		}
	}
}