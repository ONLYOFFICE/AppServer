using NUnit.Framework;

using System;
using System.Collections.Generic;

namespace ASC.Files.Tests
{
    public static class DocumentData
    {
        public static IEnumerable<TestCaseData> GetCreateFolderItems()
        {
            yield return new TestCaseData("FolderOne");
        }
        public static IEnumerable<TestCaseData> GetFolderItemsEmpty()
        {
            yield return new TestCaseData(true, 0, 0);
        }
        public static IEnumerable<TestCaseData> GetFolderItemsNotEmpty()
        {
            yield return new TestCaseData(true, 1, 1);
        }
        public static IEnumerable<TestCaseData> GetFolderInfoItems()
        {
            yield return new TestCaseData("TestFolder");
        }

        public static IEnumerable<TestCaseData> GetRenameFolderItems()
        {
            yield return new TestCaseData("FoldTest");
        }

        public static IEnumerable<TestCaseData> GetDeleteFolderItems()
        {
            yield return new TestCaseData(false, true);
        }

        public static IEnumerable<TestCaseData> GetCreateFileItems()
        {
            yield return new TestCaseData("FileOne");
        }

        public static IEnumerable<TestCaseData> GetFileInfoItems()
        {
            yield return new TestCaseData("TestFile");
        }

        public static IEnumerable<TestCaseData> GetUpdateFileItems()
        {
            yield return new TestCaseData("FileTest", 3);
        }

        public static IEnumerable<TestCaseData> GetDeleteFileItems()
        {
            yield return new TestCaseData(false, true);
        }

        public static IEnumerable<TestCaseData> GetMoveBatchItems()
        {
            yield return new TestCaseData(" [ { \"folderIds\": [ 1, 2, 3 ] }, { \"fileIds\": [ 1 , 2 ] }, { \"destFolderId\": 4 } ]");
        }

        public static IEnumerable<TestCaseData> GetCopyBatchItems()
        {
            yield return new TestCaseData(" [ { \"folderIds\": [ 6 ] }, { \"fileIds\": [ 4 , 5 ] }, { \"destFolderId\": 5 } ]");
        }

        public static IEnumerable<TestCaseData> ShareParamToFolder()
        {
            yield return new TestCaseData( false, "folder_test");
        }
        public static IEnumerable<TestCaseData> ShareParamToFile()
        {
            yield return new TestCaseData(false, "folder_test");
        }
        public static IEnumerable<TestCaseData> ShareParamToRecentFile()
        {
            yield return new TestCaseData("TestFile", false, "folder_test");
        }
        public static IEnumerable<TestCaseData> ShareParamToFileRead()
        {
            yield return new TestCaseData( false, "folder_test");
        }
        public static IEnumerable<TestCaseData> GetSharedInfo()
        {
            yield return new TestCaseData("TestFileRead");
        }
        public static IEnumerable<TestCaseData> GetSharedInfoReadAndWrite()
        {
            yield return new TestCaseData("TestFileReadAndWrite");
        }
        public static IEnumerable<TestCaseData> GetSharedFolderInfoRead()
        {
            yield return new TestCaseData("TestFolderRead");
        }
        public static IEnumerable<TestCaseData> GetSharedFolderInfoReadAndWrite()
        {
            yield return new TestCaseData("TestFolderReadAndWrite");
        }
    }
}
