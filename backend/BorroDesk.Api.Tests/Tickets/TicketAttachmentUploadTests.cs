using System.Net;
using System.Net.Http.Headers;
using BorroDesk.Api.Authorization;
using BorroDesk.Api.Tests.Infrastructure;
using FluentAssertions;

namespace BorroDesk.Api.Tests.Tickets;

public sealed class TicketAttachmentUploadTests(CustomWebApplicationFactory factory)
    : IntegrationTestBase(factory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Invalid_File_Extension_Should_Return_BadRequest()
    {
        var user = await Factory.CreateUserAsync();
        var ticket = await TestData.CreateTicketAsync(
            user,
            "Upload ticket",
            "Ticket used for upload validation tests.");
        using var client = ClientFactory.CreateClientForUser(user, ApplicationRoles.User);
        using var content = CreateMultipartContent("malware.exe", "application/octet-stream", [0x4D, 0x5A]);

        var response = await client.PostAsync($"/api/tickets/{ticket.Id}/attachments", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var attachmentCount = await TestData.CountAttachmentsAsync(ticket.Id);
        attachmentCount.Should().Be(0);
    }

    [Fact]
    public async Task Invalid_File_Signature_Should_Return_BadRequest()
    {
        var user = await Factory.CreateUserAsync();
        var ticket = await TestData.CreateTicketAsync(
            user,
            "Upload ticket",
            "Ticket used for upload validation tests.");
        using var client = ClientFactory.CreateClientForUser(user, ApplicationRoles.User);
        using var content = CreateMultipartContent(
            "image.png",
            "image/png",
            "This is not a genuine PNG file."u8.ToArray());

        var response = await client.PostAsync($"/api/tickets/{ticket.Id}/attachments", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var attachmentCount = await TestData.CountAttachmentsAsync(ticket.Id);
        attachmentCount.Should().Be(0);
    }

    private static MultipartFormDataContent CreateMultipartContent(string fileName, string contentType, byte[] bytes)
    {
        var multipartContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipartContent.Add(fileContent, "file", fileName);

        return multipartContent;
    }
}
