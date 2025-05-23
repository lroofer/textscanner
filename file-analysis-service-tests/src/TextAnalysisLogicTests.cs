using System;
using System.Reflection;
using FileAnalysisService.Services;
using Xunit;

namespace FileAnalysisService.Tests
{
    public class TextAnalysisTests
    {
        private readonly FileAnalyzer _analyzer;
        
        public TextAnalysisTests()
        {
            _analyzer = new FileAnalyzer(null, null, null, null);
        }
        
        [Fact]
        public void AnalyzeText_EmptyString_ReturnsZeroCounts()
        {
            string text = "";
            
            var result = InvokeAnalyzeText(text);
            
            Assert.Equal(0, result.ParagraphCount);
            Assert.Equal(0, result.WordCount);
            Assert.Equal(0, result.CharacterCount);
        }
        
        [Fact]
        public void AnalyzeText_SingleWord_ReturnsCorrectCounts()
        {
            string text = "Hello";
            
            var result = InvokeAnalyzeText(text);
            
            Assert.Equal(1, result.ParagraphCount);
            Assert.Equal(1, result.WordCount);
            Assert.Equal(5, result.CharacterCount);
        }
        
        [Fact]
        public void AnalyzeText_SingleParagraph_ReturnsCorrectCounts()
        {
            string text = "This is a simple paragraph with multiple words.";
            
            var result = InvokeAnalyzeText(text);
            
            Assert.Equal(1, result.ParagraphCount);
            Assert.Equal(8, result.WordCount);
            Assert.Equal(47, result.CharacterCount);
        }
        
        [Fact]
        public void AnalyzeText_MultipleParagraphs_ReturnsCorrectCounts()
        {
            string text = "This is the first paragraph.\n\nThis is the second paragraph.\n\nAnd this is the third.";
            
            var result = InvokeAnalyzeText(text);
            
            Assert.Equal(3, result.ParagraphCount);
            Assert.Equal(15, result.WordCount);
            Assert.Equal(83, result.CharacterCount);
        }
        
        [Fact]
        public void AnalyzeText_DifferentLineEndings_HandlesProperly()
        {
            string text = "First paragraph.\r\n\r\nSecond paragraph.\r\nStill second paragraph.\r\n\r\nThird paragraph.";
            
            var result = InvokeAnalyzeText(text);
            
            Assert.Equal(3, result.ParagraphCount);
            Assert.Equal(9, result.WordCount);
            Assert.Equal(82, result.CharacterCount);
        }
        
        [Fact]
        public void AnalyzeText_Numbers_CountedAsWords()
        {
            string text = "Numbers like 123 and 456 are counted as words.";
            
            var result = InvokeAnalyzeText(text);
            
            Assert.Equal(1, result.ParagraphCount);
            Assert.Equal(9, result.WordCount);
            Assert.Equal(46, result.CharacterCount);
        }
        
        [Fact]
        public void IsTextFile_TextContentTypes_ReturnsTrue()
        {
            Assert.True(InvokeIsTextFile("text/plain", "file.txt"));
            Assert.True(InvokeIsTextFile("text/html", "file.html"));
            Assert.True(InvokeIsTextFile("application/json", "file.json"));
            Assert.True(InvokeIsTextFile("application/xml", "file.xml"));
        }
        
        [Fact]
        public void IsTextFile_NonTextContentTypes_ReturnsFalse()
        {
            Assert.False(InvokeIsTextFile("image/png", "file.png"));
            Assert.False(InvokeIsTextFile("image/jpeg", "file.jpg"));
            Assert.False(InvokeIsTextFile("application/pdf", "file.pdf"));
            Assert.False(InvokeIsTextFile("application/octet-stream", "file.bin"));
        }
        
        [Fact]
        public void IsTextFile_TextExtensions_ReturnsTrue()
        {
            Assert.True(InvokeIsTextFile("application/octet-stream", "file.txt"));
            Assert.True(InvokeIsTextFile("application/octet-stream", "file.csv"));
            Assert.True(InvokeIsTextFile("application/octet-stream", "file.json"));
            Assert.True(InvokeIsTextFile("application/octet-stream", "file.xml"));
            Assert.True(InvokeIsTextFile("application/octet-stream", "file.md"));
            Assert.True(InvokeIsTextFile("application/octet-stream", "file.html"));
            Assert.True(InvokeIsTextFile("application/octet-stream", "file.htm"));
            Assert.True(InvokeIsTextFile("application/octet-stream", "file.css"));
            Assert.True(InvokeIsTextFile("application/octet-stream", "file.js"));
            Assert.True(InvokeIsTextFile("application/octet-stream", "file.ts"));
            Assert.True(InvokeIsTextFile("application/octet-stream", "file.log"));
        }
        
        [Fact]
        public void IsTextFile_NonTextExtensions_ReturnsFalse()
        {
            Assert.False(InvokeIsTextFile("application/octet-stream", "file.png"));
            Assert.False(InvokeIsTextFile("application/octet-stream", "file.jpg"));
            Assert.False(InvokeIsTextFile("application/octet-stream", "file.pdf"));
            Assert.False(InvokeIsTextFile("application/octet-stream", "file.docx"));
            Assert.False(InvokeIsTextFile("application/octet-stream", "file.xlsx"));
            Assert.False(InvokeIsTextFile("application/octet-stream", "file.zip"));
            Assert.False(InvokeIsTextFile("application/octet-stream", "file.exe"));
        }
        
        private (int ParagraphCount, int WordCount, int CharacterCount) InvokeAnalyzeText(string text)
        {
            var methodInfo = typeof(FileAnalyzer).GetMethod("AnalyzeText", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            return ((int, int, int))methodInfo.Invoke(_analyzer, new object[] { text });
        }
        
        private bool InvokeIsTextFile(string contentType, string fileName)
        {
            var methodInfo = typeof(FileAnalyzer).GetMethod("IsTextFile", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            return (bool)methodInfo.Invoke(_analyzer, new object[] { contentType, fileName });
        }
    }
}
