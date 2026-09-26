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
        var url = DecodeBase64String(request.UrlB64);
        var page = DecodeBase64String(request.PageB64);

        var emailsList = ExtractEmails(page);

        var decryptedPlainText = DecryptText(
            request.EncryptedTextBytesB64,
            request.KeyBytesB64);

        var document = await ParseHtmlAsync(page, cancellationToken);

        var elements = document.QuerySelectorAll(request.Selector);

        var (elementsAttrList, htmlElements) = ExtractElements(elements, request);

        await InsertElementsAsync(
            cancellationToken,
            elementsAttrList,
            htmlElements);

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

    private string DecodeBase64String(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    private List<string> ExtractEmails(string page)
    {
        return EmailRegex
            .Matches(page)
            .Select(match => match.Value)
            .ToList();
    }

    private string DecryptText(string encryptedTextBytesB64, string keyBytesB64)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedTextBytesB64);
        var keyBytes = Convert.FromBase64String(keyBytesB64);

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
}