﻿using FtpMonitoringService.Models;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Tenders.Core.Abstractions.Services;

namespace FtpMonitoringService
{
    public class FtpClient
    {
        private readonly ILoggerService logger;

        public static FtpClient Get(ILoggerService logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return new FtpClient(logger);
        }

        private FtpClient(ILoggerService logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public FtpFile[] ListDirectoryFields(string dirPath, string username, string password)
        {
            //logger.Log($"ListDirectoryFiels at {dirPath} with creds: {username}:{password}");
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(dirPath);
            request.Credentials = new NetworkCredential(username, password);
#if DEBUG
            request.EnableSsl = false;
#else
            request.EnableSsl = false;
#endif
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            string responseText;
            using (var response = (FtpWebResponse)request.GetResponse())
            using (var responseBody = new MemoryStream())
            {
                response.GetResponseStream().CopyTo(responseBody);
                responseText = Encoding.Default.GetString(responseBody.ToArray());
            };

            //logger.Log($"ListDirectoryFiels responseText: {responseText}");
            return responseText.Split("\r\n").Where(l => !string.IsNullOrEmpty(l)).Select(l => _lineToFile(dirPath, l)).Where(f => !f.IsDirectory).ToArray();
        }

        public ZipArchiveEntry[] GetArchiveEntries(string filePath, string username, string password)
        {
            //logger.Log($"GetArchiveEntries at {filePath} with creds: {username}:{password}");
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(filePath);
            request.Credentials = new NetworkCredential(username, password);
#if DEBUG
            request.EnableSsl = false;
#else
            request.EnableSsl = false;
#endif
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            ZipArchiveEntry[] entries;
            using (var response = (FtpWebResponse)request.GetResponse())
            using (var responseBody = new MemoryStream())
            {
                response.GetResponseStream().CopyTo(responseBody);
                var archive = new ZipArchive(responseBody, ZipArchiveMode.Read);
                entries = new ZipArchiveEntry[archive.Entries.Count];
                archive.Entries.CopyTo(entries, 0);
            }

            return entries;
        }

        private FtpFile _lineToFile(string parentDir, string lineToFile)
        {
            //logger.Log($"_lineToFile parentDir: {parentDir} lineToFile is {HttpUtility.UrlEncode(lineToFile)} and contains {lineToFile.Split('\t').Length}");
            var file = new FtpFile();

#if DEBUG
            var parts = Regex.Replace(lineToFile, @"\s+", " ").Split(" ");

            file.Name = parts[3];
            file.DateModified = DateTime.ParseExact(parts[0] + " " + parts[1], "MM-dd-yy hh:mmtt", CultureInfo.GetCultureInfoByIetfLanguageTag("en"));
            if (parts[2].Equals("<DIR>"))
            {
                file.IsDirectory = true;
                file.Name += "/";
            }
            else
            {
                file.Size = long.Parse(parts[2]);
            }
#else
            var isDir = lineToFile.StartsWith("d");
            var cutted = lineToFile.Substring(29).Trim();
            var parts = cutted.Split(' ');
            var changeDate = _parseDate(parts);

            file.Name = parts.Last() + (isDir ? "/" : "");
            file.IsDirectory = isDir;
            file.DateModified = changeDate;
            file.Size = long.Parse(parts[0]);

            // Для уверенности проверим полученную дату
            if (changeDate > DateTime.Now)
                logger.Log($"Дата изменения файла {file.Name} - {changeDate} больше текущей!");
#endif

            return file;
        }

        private DateTime _parseDate(string[] parts)
        {
            var isContainsTime = parts.Length < 6;
            var year = isContainsTime ? $"{DateTime.Now.Year} {parts[3]}" : parts[4]; // Добавляем год если указано только время
            var changeDate = DateTime.Parse($"{parts[1]} {parts[2]} {year}", CultureInfo.GetCultureInfoByIetfLanguageTag("en"));
            // Если месяц больше текущего - значит это данные данные за прошлый год (догадка)
            if (changeDate.Month > DateTime.Now.Month)
                changeDate = changeDate.AddYears(-1);

            return changeDate;
        }

    }
}
