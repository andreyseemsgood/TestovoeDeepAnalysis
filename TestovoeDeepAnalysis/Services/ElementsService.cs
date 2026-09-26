using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Dapper;
using Npgsql;
using TestovoeDeepAnalysis.Models;

namespace TestovoeDeepAnalysis.Services;

public class ElementsService
{
    private readonly NpgsqlConnection _connection;
    private static readonly Regex EmailRegex = new(
        @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}",
        RegexOptions.Compiled);
    
    public ElementsService(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<ElementsResponse> ProcessAsync(ElementsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var url = DecodeBase64String(request.UrlB64, "url_b64");
            var page = DecodeBase64String(request.PageB64, "page_b64");

            var emailsList = ExtractEmails(page);

            var decryptedPlainText = DecryptText(
                request.EncryptedTextBytesB64,
                request.KeyBytesB64);

            var document = await ParseHtmlAsync(page, cancellationToken);

            var elements = document.QuerySelectorAll(request.Selector);

            var (elementsAttrList, htmlElements) = ExtractElements(elements, request);

            await InsertElementsAsync(cancellationToken, elementsAttrList, htmlElements);

            return new ElementsResponse
            {
                IsError = 0,
                ErrorCode = 0,
                Url = url,
                ElementsCount = elements.Length,
                EmailsCount = emailsList.Count,
                ElementsAttrList = elementsAttrList,
                EmailsList = emailsList,
                DecryptedPlainText = decryptedPlainText
            };
        }
        catch (FormatException ex)
        {
            return CreateErrorResponse(400, ex.Message);
        }
        catch (CryptographicException ex)
        {
            return CreateErrorResponse(400, ex.Message);
        }
        catch (DomException ex)
        {
            return CreateErrorResponse(400, "Invalid CSS selector.");
        }
        catch (NpgsqlException)
        {
            return CreateErrorResponse(500, "Database error.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return CreateErrorResponse(500, "Internal server error.");
        }

    }
    
    private List<string> ExtractEmails(string page)
    {
        return EmailRegex
            .Matches(page)
            .Select(match => match.Value)
            .ToList();
    }

    
    private string DecodeBase64String(string base64, string fieldName)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            throw new FormatException($"Invalid Base64 data in field '{fieldName}'.");
        }
    }
    
    
    private byte[] DecodeBase64Bytes(string base64, string fieldName)
    {
        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            throw new FormatException($"Invalid Base64 data in field '{fieldName}'.");
        }
    }
    
    
    private string DecryptText(string encryptedTextBytesB64, string keyBytesB64)
    {
        var encryptedBytes = DecodeBase64Bytes(encryptedTextBytesB64, "encrypted_text_bytes_b64");
        var keyBytes = DecodeBase64Bytes(keyBytesB64, "key_bytes_b64");

        if (keyBytes.Length != 32)
        {
            throw new CryptographicException("AES-256 key must contain 32 bytes.");
        }
        if (encryptedBytes.Length % 16 != 0)
        {
            throw new CryptographicException("Encrypted data length must be a multiple of 16 bytes.");
        }
        
        using var aes = Aes.Create();

        aes.Key = keyBytes;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        using var decryptor = aes.CreateDecryptor();

        var decryptedBytes = decryptor.TransformFinalBlock(
            encryptedBytes,
            0,
            encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    
    private async Task<IDocument> ParseHtmlAsync(string page, CancellationToken cancellationToken)
    {
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);

        return await context.OpenAsync(
            req => req.Content(page),
            cancellationToken);
    }

    
    private (List<string> ElementsAttrList, List<string> HtmlElements) ExtractElements(
        IHtmlCollection<IElement> elements,
        ElementsRequest request)
    {
        var elementsAttrList = new List<string>();
        var htmlElements = new List<string>();

        foreach (var element in elements)
        {
            var attributeValue = element.GetAttribute(request.Attribute!);

            if (attributeValue is null)
            {
                continue;
            }

            elementsAttrList.Add(attributeValue);
            htmlElements.Add(element.OuterHtml);
        }

        return (elementsAttrList, htmlElements);
    }
    

    private async Task InsertElementsAsync(
        CancellationToken cancellationToken,
        List<string> elementsAttrList,
        List<string> htmlElements)
    {
        await _connection.OpenAsync(cancellationToken);

        const string sql = """
                           INSERT INTO elements (attribute_value, html_element)
                           VALUES (@AttributeValue, @HtmlElement);
                           """;

        for (var i = 0; i < elementsAttrList.Count; i++)
        {
            var command = new CommandDefinition(
                sql,
                new
                {
                    AttributeValue = elementsAttrList[i],
                    HtmlElement = htmlElements[i]
                },
                cancellationToken: cancellationToken);

            await _connection.ExecuteAsync(command);
        }
    }
    
    private ElementsResponse CreateErrorResponse(int errorCode, string errorMessage)
    {
        return new ElementsResponse
        {
            IsError = 1,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}