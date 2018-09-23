﻿namespace Shapeshifter.WindowsDesktop.Data.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Interfaces;

    using Services.Clipboard.Interfaces;
    using Services.Files.Interfaces;

    class ZipFilesAction: IZipFilesAction
    {
        readonly IAsyncFilter asyncFilter;
        readonly IFileManager fileManager;
        readonly IClipboardInjectionService clipboardInjectionService;

        public ZipFilesAction(
            IAsyncFilter asyncFilter,
            IFileManager fileManager,
            IClipboardInjectionService clipboardInjectionService)
        {
            this.asyncFilter = asyncFilter;
            this.fileManager = fileManager;
            this.clipboardInjectionService = clipboardInjectionService;
		}

		public string Title => "Copy as compressed folder";

        public byte Order => 75;

        public async Task<bool> CanPerformAsync(
            IClipboardDataPackage package)
        {
            var supportedData = await GetSupportedData(package);
            return supportedData.Any();
        }

        async Task<IReadOnlyCollection<IClipboardData>> GetSupportedData(
            IClipboardDataPackage package)
        {
            var supportedData = await asyncFilter.FilterAsync(package.Contents, CanPerformAsync);
            return supportedData;
        }

        static async Task<bool> CanPerformAsync(
            IClipboardData data)
        {
            return
                data is IClipboardFileData ||
                data is IClipboardFileCollectionData;
        }

        public async Task PerformAsync(
            IClipboardDataPackage processedData)
        {
            var supportedDataCollection = await GetSupportedData(processedData);
            var firstSupportedData = supportedDataCollection.FirstOrDefault();

            var zipFilePath = await ZipDataAsync(firstSupportedData);
            await clipboardInjectionService.InjectFilesAsync(zipFilePath);
        }

        async Task<string> ZipFileCollectionDataAsync(params IClipboardFileData[] fileDataItems)
        {
            if (fileDataItems.Length == 0)
            {
                throw new ArgumentException(
                    "There must be at least one item to compress.",
                    nameof(fileDataItems));
            }

            var filePaths = fileDataItems
                .Select(x => x.FullPath)
                .ToArray();
            var commonPath = fileManager.FindCommonFolderFromPaths(filePaths);
            var directoryName = Path.GetFileName(commonPath);
            var directoryPath = await fileManager.PrepareTemporaryFolderAsync(directoryName);
            await CopyFilesToTemporaryFolderAsync(
                fileDataItems,
                directoryPath);

            var zipFile = await ZipDirectoryAsync(directoryPath);
            return zipFile;
        }

        async Task CopyFilesToTemporaryFolderAsync(
            IEnumerable<IClipboardFileData> fileDataItems,
            string directory)
        {
            foreach (var fileData in fileDataItems)
            {
                await CopyFileToTemporaryFolderAsync(directory, fileData);
            }
        }

        async Task CopyFileToTemporaryFolderAsync(string directory, IClipboardFileData fileData)
        {
            var destinationFilePath = Path.Combine(
                directory,
                fileData.FileName);
            await fileManager.DeleteFileIfExistsAsync(destinationFilePath);
            File.Copy(fileData.FullPath, destinationFilePath);
        }

        async Task<string> ZipDirectoryAsync(string directory)
        {
            var directoryName = Path.GetFileName(directory);
            var compressedFolderDirectory = await fileManager.PrepareTemporaryFolderAsync($"Compressed folders");
            var zipFile = Path.Combine(compressedFolderDirectory, $"{directoryName}.zip");

            await fileManager.DeleteFileIfExistsAsync(zipFile);
            ZipFile.CreateFromDirectory(directory, zipFile);

            return zipFile;
        }

        async Task<string> ZipDataAsync(IClipboardData data)
        {
            var clipboardFileData = data as IClipboardFileData;
            if (clipboardFileData != null)
            {
                return await ZipFileCollectionDataAsync(clipboardFileData);
            }

            var clipboardFileCollectionData = data as IClipboardFileCollectionData;
            if (clipboardFileCollectionData != null)
            {
                return await ZipFileCollectionDataAsync(
                    clipboardFileCollectionData
                        .Files
                        .ToArray());
            }

            throw new InvalidOperationException("Unknown data format.");
        }
    }
}