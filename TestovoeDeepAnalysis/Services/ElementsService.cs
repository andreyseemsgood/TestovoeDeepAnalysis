using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using Dapper;
using Npgsql;
using TestovoeDeepAnalysis.Models;

namespace TestovoeDeepAnalysis.Services;

public class ElementsService
{
    private readonly NpgsqlConnection _connection;

    public ElementsService(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<ElementsResponse> ProcessAsync(
        ElementsRequest request,
        CancellationToken cancellationToken)
    {
        var urlBytes = Convert.FromBase64String(request.UrlB64!);
        var url = Encoding.UTF8.GetString(urlBytes);

        var pageBytes = Convert.FromBase64String(request.PageB64!);
        var page = Encoding.UTF8.GetString(pageBytes);

        var encryptedBytes = Convert.FromBase64String(request.EncryptedTextBytesB64!);

        var keyBytes = Convert.FromBase64String(request.KeyBytesB64!);

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);

        var document = await context.OpenAsync(
            req => req.Content(page),
            cancellationToken);

        var elements = document.QuerySelectorAll(request.Selector!);

        var elementsAttrList = new List<string>();
        var htmlElements = new List<string>();
        GetElementsAndAttributes(elements, request, elementsAttrList, htmlElements);

        await InsertElementsAsync(cancellationToken, elementsAttrList, htmlElements);

        var response = new ElementsResponse
        {
            IsError = 0,
            ErrorCode = 0,
            Url = url,
            ElementsCount = elements.Length,
            ElementsAttrList = elementsAttrList
        };

        return response;
    }

    private void GetElementsAndAttributes(IHtmlCollection<IElement> elements, ElementsRequest request, List<string> elementsAttrList, List<string> htmlElements)
    {
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