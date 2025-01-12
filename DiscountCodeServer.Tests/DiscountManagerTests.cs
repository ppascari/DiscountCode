using System;
using System.IO;
using System.Linq;
using DiscountCodeServer;
using Xunit;

namespace DiscountCodeServer.Tests
{
    public class DiscountManagerTests
    {
        [Fact]
        public void GenerateCodes_ShouldGenerateExactCount()
        {
            // Arrange
            // Create a new DiscountManager instance with a temporary file path
            // to isolate this test from other runs or real data.
            string tempFilePath = Path.GetTempFileName();
            var manager = CreateDiscountManagerWithTempFile(tempFilePath);

            // Act
            var codes = manager.GenerateCodes(5, 7);

            // Assert
            // Ensure the correct number of codes are generated
            Assert.Equal(5, codes.Count);
            // Ensure all codes are of the specified length (7 characters)
            Assert.All(codes, code => Assert.Equal(7, code.Length));

            // Cleanup
            CleanupTempFile(tempFilePath);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(8)]
        public void GenerateCodes_ShouldGenerateCorrectLength(int length)
        {
            // Arrange
            // Create a temporary file to store codes during this test.
            string tempFilePath = Path.GetTempFileName();
            var manager = CreateDiscountManagerWithTempFile(tempFilePath);

            // Act
            var codes = manager.GenerateCodes(3, length);

            // Assert
            // Ensure the correct number of codes are generated
            Assert.Equal(3, codes.Count);
            // Ensure all codes have the specified length (7 or 8 characters)
            Assert.All(codes, code => Assert.Equal(length, code.Length));

            // Cleanup
            CleanupTempFile(tempFilePath);
        }

        [Fact]
        public void GenerateCodes_ShouldBeUnique()
        {
            // Arrange
            // Create a temporary file to store codes during this test.
            string tempFilePath = Path.GetTempFileName();
            var manager = CreateDiscountManagerWithTempFile(tempFilePath);

            // Act
            // Generate 10 codes of length 7
            var codes = manager.GenerateCodes(10, 7);

            // Assert
            // Ensure all generated codes are unique
            Assert.Equal(10, codes.Distinct().Count());

            // Cleanup
            CleanupTempFile(tempFilePath);
        }

        [Fact]
        public void UseCode_ShouldReturn_Success_ForValidCode()
        {
            // Arrange
            // Create a temporary file to store codes during this test.
            string tempFilePath = Path.GetTempFileName();
            var manager = CreateDiscountManagerWithTempFile(tempFilePath);

            // Generate 1 code, then attempt to use it
            var codes = manager.GenerateCodes(1, 7);
            string codeToUse = codes[0];

            // Act
            var result = manager.UseCode(codeToUse);

            // Assert
            // Ensure the code is successfully marked as used
            Assert.Equal(UseCodeResult.Success, result);

            // Cleanup
            CleanupTempFile(tempFilePath);
        }

        [Fact]
        public void UseCode_ShouldReturn_AlreadyUsed_IfUsedAgain()
        {
            // Arrange
            // Create a temporary file to store codes during this test.
            string tempFilePath = Path.GetTempFileName();
            var manager = CreateDiscountManagerWithTempFile(tempFilePath);

            // Generate a single code and use it twice
            var codes = manager.GenerateCodes(1, 7);
            string codeToUse = codes[0];

            // First usage should succeed
            var firstResult = manager.UseCode(codeToUse);
            // Second usage should return "AlreadyUsed"
            var secondResult = manager.UseCode(codeToUse);

            // Assert
            Assert.Equal(UseCodeResult.Success, firstResult);
            Assert.Equal(UseCodeResult.AlreadyUsed, secondResult);

            // Cleanup
            CleanupTempFile(tempFilePath);
        }

        [Fact]
        public void UseCode_ShouldReturn_CodeNotFound_ForInvalidCode()
        {
            // Arrange
            // Create a temporary file to store codes during this test.
            string tempFilePath = Path.GetTempFileName();
            var manager = CreateDiscountManagerWithTempFile(tempFilePath);

            // Act
            // Attempt to use an invalid (non-existent) code
            var result = manager.UseCode("INVALIDCODE");

            // Assert
            // Ensure the result indicates the code was not found
            Assert.Equal(UseCodeResult.CodeNotFound, result);

            // Cleanup
            CleanupTempFile(tempFilePath);
        }

        // Helper method to create a DiscountManager instance with a temporary file path
        private DiscountManager CreateDiscountManagerWithTempFile(string path)
        {
            // Use a derived class to override the file path for tests
            return new DiscountManagerTestable(path);
        }

        // Helper method to delete the temporary file after testing
        private void CleanupTempFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        // Test subclass to override the _filePath field in DiscountManager
        private class DiscountManagerTestable : DiscountManager
        {
            public DiscountManagerTestable(string testFilePath)
            {
                // Override the file path with the test-specific path
                base._filePath = testFilePath;
                // Reload codes from the test file path
                base._codes = LoadCodesFromFile();
            }
        }
    }
}