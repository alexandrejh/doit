﻿using System;
using System.Xml;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Data;
using ICSharpCode.SharpZipLib.Zip;

namespace DoIt
{
	internal class Util
	{
		public static String GetFileSize(Int64 lengthInBytes, Int32 decimalPlaces = 2)
		{
			var value = Convert.ToDecimal(lengthInBytes);
			if (value < 1024)
				return Math.Round(value, decimalPlaces) + " bytes";
			value /= 1024m;
			if (value < 1024)
				return Math.Round(value, decimalPlaces) + " KB";
			value /= 1024m;
			if (value < 1024)
				return Math.Round(value, decimalPlaces) + " MB";
			value /= 1024m;
			return Math.Round(value, decimalPlaces) + " GB";
		}

		public static T GetEnumValue<T>(string value, T defaultValue) where T:struct
		{
			if (string.IsNullOrEmpty(value))
				return defaultValue;
			var lstValues1 = value.Split(new char[]{',',';','|',' '}, StringSplitOptions.RemoveEmptyEntries);
			var lstValues2 = Enum.GetNames(typeof(T));
			if (!lstValues1.All(v => lstValues2.Any(s => s.ToLower() == v.ToLower())))
				return defaultValue;
			return (T)Enum.Parse(typeof(T), string.Join(",", lstValues1), true);
		}

		public static XmlNode[] GetChildNodes(XmlNode node, params string[] childNames)
		{
			if (node == null)
				return null;
			if(childNames == null || childNames.Length == 0 || childNames.All(n => string.IsNullOrEmpty(n)))
				return node.ChildNodes.Cast<XmlNode>().ToArray();
			return node.ChildNodes.Cast<XmlNode>().Where(n => childNames.Any(n2 => n2 != null && n.Name.ToLower() == n2.ToLower())).ToArray();
		}

		public static XmlNode GetChildNode(XmlNode node, params string[] childNames)
		{
			var lst = GetChildNodes(node, childNames);
			return lst == null ? null : lst.FirstOrDefault();
		}

		public static String GetStr(XmlNode node, String dataName, String defaultValue = null)
		{
			if (node == null)
				return defaultValue;
			var atr = node.Attributes[dataName];
			if (atr != null && !String.IsNullOrEmpty(atr.Value))
				return atr.Value;
			var subn = GetChildNode(node, dataName);
			return (subn == null || String.IsNullOrEmpty(subn.InnerText)) ? defaultValue : subn.InnerText;
		}

		public static String GetContentType(String filename, Boolean isExtension = false)
		{
			var ext = isExtension ? filename : Path.GetExtension(filename);
			switch (ext.ToLower()){
				case ".pdf": return "application/pdf";
				case ".zip": return "application/zip";
				case ".js": return "application/javascript";
				case ".gif": return "image/gif";
				case ".jpg": return "image/jpeg";
				case ".jpeg": return "image/jpeg";
				case ".png": return "image/png";
				case ".ico": return "image/x-icon";
				case ".tif": return "image/tiff";
				case ".tiff": return "image/tiff";
				case ".eml": return "message/rfc822";
				case ".mp4": return "video/mp4";
				case ".mp3": return "audio/mpeg";
				case ".mov": return "video/quicktime";
				case ".mpg": return "video/mpeg";
				case ".avi": return "video/x-msvideo";
				case ".wmv": return "video/x-ms-wmv";
				case ".xls": return "application/vnd.ms-excel";
				case ".doc": return "application/msword";
				case ".ppt": return "application/vnd.ms-powerpoint";
				case ".pps": return "application/vnd.ms-powerpoint";
				case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
				case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
				case ".pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
				case ".xltx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.template";
				case ".dotx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.template";
				case ".ppsx": return "application/vnd.openxmlformats-officedocument.presentationml.slideshow";
				case ".rtf": return "application/rtf";
				case ".css": return "text/css";
				case ".csv": return "text/csv";
				case ".txt": return "text/plain";
				case ".xml": return "text/xml";
				case ".htm": return "text/html";
				case ".html": return "text/html";
			}
			return "application/octet-stream";
		}

		public static String GetTempFileName(String extension = null)
		{
			var temp = Path.GetTempFileName();
			if (String.IsNullOrEmpty(extension))
				return temp;
			var filename = Path.GetFileNameWithoutExtension(temp);
			return Path.Combine(Path.GetDirectoryName(temp), filename + (extension.StartsWith(".") ? extension : "." + extension));
		}

		public static string GetConfigData(string configStr, string dataType, bool toLower = false, string defaultValue = null)
		{
			if (String.IsNullOrEmpty(configStr) || String.IsNullOrEmpty(dataType))
				return null;
			var allData = configStr.Split(new Char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var str in allData)
				if (str.Trim().ToLower().StartsWith(dataType.ToLower() + "=")){
					var data = str.Split(new Char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
					if (data.Length == 2){
						var value = data[1];
						if (String.IsNullOrEmpty(value))
							return defaultValue;
						return toLower ? value.ToLower() : value;
					}
				}
			return null;
		}

		public static string GetArg(string[] args, string key, bool toLower = false)
		{
			if (args == null || args.Length == 0)
				return null;
			foreach (var arg in args){
				if (String.IsNullOrEmpty(arg))
					continue;
				var str = arg;
				if (!str.StartsWith("/") && !str.StartsWith("-") && !str.StartsWith("--"))
					continue;
				if (str.StartsWith("/"))
					str = arg.Remove(0, 1);
				if (str.StartsWith("--"))
					str = arg.Remove(0, 2);
				if (str.StartsWith("-"))
					str = arg.Remove(0, 1);
				if (!str.StartsWith(key, StringComparison.InvariantCultureIgnoreCase))
					continue;
				var index = str.IndexOf("=");
				if (index == -1 || index == str.Length - 1)
					return toLower ? key.ToLower() : key;
				var value = str.Substring(index + 1);
				if (String.IsNullOrEmpty(value))
					return value;
				value = value.Trim(new Char[] { '"', ' ' });
				return toLower ? value.ToLower() : value;
			}
			return null;
		}

		public static object GetValue(object value, string type)
		{
			if (value == null || string.IsNullOrEmpty(type))
				return value;
			switch(type.ToLower()){
				case "byte": return Convert.ToByte(value);
				case "short": return Convert.ToInt16(value);
				case "int": return Convert.ToInt32(value);
				case "long": return Convert.ToInt64(value);
				case "decimal": return Convert.ToDecimal(value);
				case "float": return Convert.ToSingle(value);
				case "double": return Convert.ToDouble(value);
				case "string": return Convert.ToString(value);
				case "datetime": return value is DateTime ? new Nullable<DateTime>(Convert.ToDateTime(value)) : ParseDateTime(Convert.ToString(value));
				case "datetimeoffset": return value is DateTime ? new Nullable<DateTimeOffset>(new DateTimeOffset(Convert.ToDateTime(value))) : ParseDateTimeOffset(Convert.ToString(value));
				case "bool": return Convert.ToBoolean(value);
			}
			return value;
		}

		public static Type GetType(string type)
		{
			if (string.IsNullOrEmpty(type))
				return typeof(object);
			switch(type.ToLower()){
				case "byte": return typeof(byte);
				case "short": return typeof(short);
				case "int": return typeof(int);
				case "long": return typeof(long);
				case "decimal": return typeof(decimal);
				case "float": return typeof(float);
				case "double": return typeof(double);
				case "string": return typeof(string);
				case "datetime": return typeof(DateTime);
				case "datetimeoffset": return typeof(DateTimeOffset);
				case "bool": return typeof(bool);
			}
			return typeof(object);
		}

		public static string GetStrData(string str, string tag, object data)
		{
			str = str.Replace("{" + tag + "}", string.Format("{0}", data is DateTime ? GetDateTimeString((DateTime)data) : (data is DateTimeOffset ? GetDateTimeOffsetString((DateTimeOffset)data) : data)));
			while (true){
				var index1 = str.ToLower().IndexOf("{" + tag.ToLower() + ":");
				var index2 = str.IndexOf("}", index1 + 1);
				if (index1 == -1 || index2 == -1)
					break;
				var format = str.Substring(index1 + tag.Length + 2, index2 - index1 - (tag.Length + 2));
				if (!string.IsNullOrEmpty(format)){
					str = str.Remove(index1, index2 - index1 + 1);
					if (format.ToLower() == "filename")
						str = str.Insert(index1, Path.GetFileName(Convert.ToString(data)));
					else if (format.ToLower() == "fileextension")
						str = str.Insert(index1, Convert.ToString(data).GetFileExtension());
					else
						str = str.Insert(index1, string.Format("{0:" + format + "}", data));
				}
			}
			return str;
		}

		public static string GetDateTimeString(DateTime date)
		{
			if (date.Hour == 0 && date.Minute == 0 && date.Second == 0 && date.Millisecond == 0)
				return date.ToString("yyyy-MM-dd");
			if(date.Millisecond == 0)
				return date.ToString("yyyy-MM-dd HH:mm:ss");
			return date.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
		}

		public static string GetDateTimeOffsetString(DateTimeOffset date)
		{
			if (date.Hour == 0 && date.Minute == 0 && date.Second == 0 && date.Millisecond == 0)
				return date.ToString("yyyy-MM-dd");
			if(date.Millisecond == 0)
				return date.ToString("yyyy-MM-dd HH:mm:sszzz");
			return date.ToString("yyyy-MM-dd HH:mm:ss.fffffffzzz");
		}

		public static DateTime? ParseDateTime(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2} \\d{2}\\:\\d{2}:\\d{2}\\.\\d{7}$"))
				return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss.fffffff", null);
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2} \\d{2}\\:\\d{2}:\\d{2}\\.\\d{3}$"))
				return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss.fff", null);
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2} \\d{2}\\:\\d{2}:\\d{2}$"))
				return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss", null);
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2}$"))
				return DateTime.ParseExact(value, "yyyy-MM-dd", null);
			return null;
		}

		public static DateTimeOffset? ParseDateTimeOffset(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2} \\d{2}\\:\\d{2}:\\d{2}\\.\\d{7}(\\+|\\-)\\d{2}:\\d{2}$"))
				return DateTimeOffset.ParseExact(value, "yyyy-MM-dd HH:mm:ss.fffffffzzz", null);
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2} \\d{2}\\:\\d{2}:\\d{2}\\.\\d{3}(\\+|\\-)\\d{2}:\\d{2}$"))
				return DateTimeOffset.ParseExact(value, "yyyy-MM-dd HH:mm:ss.fffzzz", null);
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2} \\d{2}\\:\\d{2}:\\d{2}(\\+|\\-)\\d{2}:\\d{2}$"))
				return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:sszzz", null);
			if (value.IsMatch("^\\d{4}-\\d{2}-\\d{2}$"))
				return DateTime.ParseExact(value, "yyyy-MM-dd", null);
			return null;
		}

		public static bool IsTodayInList(String days)
		{
			if (String.IsNullOrEmpty(days))
				return false;
			var array = days.Split(new string[] { ",", ";", " ", "|" }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Contains("all") || array.Contains(Convert.ToString(DateTime.Today.Day)))
				return true;
			var lst = new List<String>() { "sun", "mon", "tue", "wed", "thu", "fri", "sat" };
			if (array.Select(item => lst.IndexOf(item)).Any(index => index == Convert.ToInt32(DateTime.Today.DayOfWeek)))
				return true;
			return false;
		}

		public static DateTime GetDateTime(string timespan, DateTime? date = null)
		{
			return GetDateTimeOffset(timespan, new DateTimeOffset(date == null ? DateTime.Now : date.Value)).DateTime;
		}

		public static DateTimeOffset GetDateTimeOffset(string timespan, DateTimeOffset? date = null)
		{
			if (date == null)
				date = DateTimeOffset.Now;
			if (string.IsNullOrEmpty(timespan))
				return date.Value;
			timespan = timespan.ToLower();
			var m = Regex.Match(timespan, "^(?<operation>\\-|\\+)?\\s*(?<value>\\d+)\\s*(?<measure>millisecond|second|minute|hour|day|week|month|year)s?$");
			if (!m.Success)
				throw new ArgumentException($"Invalid GetDate parameter \"{timespan}\"");
			var operation = m.Groups["operation"].Success ? m.Groups["operation"].Value : "+";
			var value = Convert.ToInt32(m.Groups["value"].Value) * (operation == "+" ? 1 : -1);
			var measure = m.Groups["measure"].Value;
			switch(measure){
				case "millisecond": return date.Value.AddMilliseconds(value);
				case "second": return date.Value.AddSeconds(value);
				case "minute": return date.Value.AddMinutes(value);
				case "hour": return date.Value.AddHours(value);
				case "day": return date.Value.AddDays(value);
				case "week": return date.Value.AddDays(value*7);
				case "month": return date.Value.AddMonths(value);
				case "year": return date.Value.AddYears(value);
			}
			return date.Value;
		}

		public static TimeSpan GetTimeSpan(string timespan)
		{
			if (string.IsNullOrEmpty(timespan))
				return TimeSpan.Zero;
			timespan = timespan.ToLower();
			var m = Regex.Match(timespan, "^(?<value>\\d+)\\s*(?<measure>millisecond|second|minute|hour|day|week)s?$");
			if (!m.Success)
				throw new ArgumentException($"Invalid GetTimeSpan parameter \"{timespan}\"");
			var value = Convert.ToInt32(m.Groups["value"].Value);
			var measure = m.Groups["measure"].Value;
			switch(measure){
				case "millisecond": return TimeSpan.FromMilliseconds(value);
				case "second": return TimeSpan.FromSeconds(value);
				case "minute": return TimeSpan.FromMinutes(value);
				case "hour": return TimeSpan.FromHours(value);
				case "day": return TimeSpan.FromDays(value);
				case "week": return TimeSpan.FromDays(value*7);
			}
			return TimeSpan.Zero;
		}

		public static Boolean IsTimeToRun(String time)
		{
			if (String.IsNullOrEmpty(time))
				return true;
			if (Regex.IsMatch(time, "^\\d+$") && Convert.ToInt32(time) == DateTime.Now.Hour)
				return true;
			if (Regex.IsMatch(time, "^\\d+\\-\\d+$")){
				var array = time.Split('-');
				var h1 = Convert.ToInt32(array[0]);
				var h2 = Convert.ToInt32(array[1]);
				var h = DateTime.Now.Hour;
				return (h1<=h2 && h>=h1 && h<=h2) || (h1>=h2 && (h>h1 || h<h2));
			}
			return false;
		}

		public static long GetFreeSpace(string drive)
		{
			foreach (DriveInfo di in DriveInfo.GetDrives())
				if (di.IsReady && di.Name == drive)
					return di.TotalFreeSpace;
			return -1;
		}

		public static string[] GetFiles(string path, string searchPattern = "*.*", SearchOption searchOption = SearchOption.AllDirectories)
		{
			var lstAllFiles = new List<string>();
			var lstPatterns = (searchPattern??"*.*").Split(new char[]{'|'}, StringSplitOptions.RemoveEmptyEntries);
			foreach (var pattern in lstPatterns)
				lstAllFiles.AddRange(Directory.GetFiles(path, pattern, searchOption));
			return lstAllFiles.ToArray();
		}

		public static string GetFileToSend(String filename, Boolean zipFile)
		{
			if (!zipFile)
				return filename;
			var zipFilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".zip");
			if (File.Exists(zipFilename))
				File.Delete(zipFilename);
			using (var zipStream = new ZipOutputStream(File.Create(zipFilename))){
				var fi = new FileInfo(filename);
				zipStream.PutNextEntry(new ZipEntry(Path.GetFileName(filename)){DateTime=fi.LastWriteTime, Size=fi.Length});
				using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					fs.CopyTo(zipStream);
			}
			return zipFilename;
		}

		#region database helpers
		public static DataTable Select(string sql, string dbConnectionString)
		{
			Console.WriteLine("SQL: " + sql);
			using (var conn = new SqlConnection(dbConnectionString)){
				conn.Open();
				var dt = new DataTable();
				using (var da = new SqlDataAdapter(sql, dbConnectionString))
					da.Fill(dt);
				return dt;
			}
		}

		public static DataTable Select(string sql, SqlTransaction transaction)
		{
			Console.WriteLine("SQL: " + sql);
			var dt = new DataTable();
			using (var cmd = new SqlCommand(sql, transaction.Connection, transaction))
			using (var da = new SqlDataAdapter(cmd))
				da.Fill(dt);
			return dt;
		}

		public static object Scalar(string sql, string dbConnectionString)
		{
			Console.WriteLine("SQL: " + sql);
			using (var conn = new SqlConnection(dbConnectionString)){
				conn.Open();
				using (var cmd = new SqlCommand(sql, conn))
					return cmd.ExecuteScalar();
			}
		}

		public static object Scalar(string sql, SqlTransaction transaction)
		{
			Console.WriteLine("SQL: " + sql);
			using (var cmd = new SqlCommand(sql, transaction.Connection, transaction))
				return cmd.ExecuteScalar();
		}

		public static int Execute(string sql, string dbConnectionString, Int32? commandTimeout = null)
		{
			Console.WriteLine("SQL: " + sql);
			using (var conn = new SqlConnection(dbConnectionString)){
				conn.Open();
				using (var cmd = new SqlCommand(sql, conn)){
					if (commandTimeout != null && commandTimeout.Value != 0)
						cmd.CommandTimeout = commandTimeout.Value;
					return cmd.ExecuteNonQuery();
				}
			}
		}

		public static int Execute(string sql, SqlTransaction transaction, Int32? commandTimeout = null)
		{
			Console.WriteLine("SQL: " + sql);
			using (var cmd = new SqlCommand(sql, transaction.Connection, transaction)){
				if (commandTimeout != null && commandTimeout.Value != 0)
					cmd.CommandTimeout = commandTimeout.Value;
				return cmd.ExecuteNonQuery();
			}
		}
		#endregion

	}
}
