using Ceiba.Shared.DTOs;
using FluentAssertions;

namespace Ceiba.Core.Tests.DTOs;

/// <summary>
/// Unit tests for Configuration DTOs (AI, Email, Automated Reports).
/// </summary>
[Trait("Category", "Unit")]
public class ConfigurationDTOsTests
{
    #region AiConfigurationDto Tests

    [Fact(DisplayName = "AiConfigurationDto should have default values")]
    public void AiConfigurationDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new AiConfigurationDto();

        // Assert
        dto.Id.Should().Be(0);
        dto.Proveedor.Should().Be("OpenAI");
        dto.Modelo.Should().Be("gpt-4o-mini");
        dto.MaxTokens.Should().Be(1000);
        dto.Temperature.Should().Be(0.7);
        dto.Activo.Should().BeTrue();
        dto.ApiKey.Should().BeNull();
        dto.UpdatedAt.Should().BeNull();
    }

    [Fact(DisplayName = "AiConfigurationDto should store complete configuration")]
    public void AiConfigurationDto_ShouldStoreCompleteConfiguration()
    {
        // Arrange & Act
        var dto = new AiConfigurationDto
        {
            Id = 1,
            Proveedor = "AzureOpenAI",
            ApiKey = "sk-test-key",
            ApiKeyMasked = "sk-****key",
            Modelo = "gpt-4",
            Endpoint = "https://api.openai.com/v1",
            AzureEndpoint = "https://myresource.openai.azure.com",
            AzureApiVersion = "2024-02-01",
            MaxTokens = 4000,
            Temperature = 0.5,
            Activo = true
        };

        // Assert
        dto.Proveedor.Should().Be("AzureOpenAI");
        dto.AzureEndpoint.Should().Contain("azure.com");
        dto.MaxTokens.Should().Be(4000);
    }

    #endregion

    #region SaveAiConfigurationDto Tests

    [Fact(DisplayName = "SaveAiConfigurationDto should have default values")]
    public void SaveAiConfigurationDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new SaveAiConfigurationDto();

        // Assert
        dto.Proveedor.Should().Be("OpenAI");
        dto.Modelo.Should().Be("gpt-4o-mini");
        dto.MaxTokens.Should().Be(1000);
        dto.Temperature.Should().Be(0.7);
    }

    #endregion

    #region AiConnectionTestResultDto Tests

    [Fact(DisplayName = "AiConnectionTestResultDto should have default values")]
    public void AiConnectionTestResultDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new AiConnectionTestResultDto();

        // Assert
        dto.Success.Should().BeFalse();
        dto.Message.Should().BeEmpty();
        dto.ResponseTimeMs.Should().BeNull();
    }

    [Fact(DisplayName = "AiConnectionTestResultDto should store successful test result")]
    public void AiConnectionTestResultDto_ShouldStoreSuccessfulTestResult()
    {
        // Arrange & Act
        var dto = new AiConnectionTestResultDto
        {
            Success = true,
            Message = "Connection successful",
            ResponseTimeMs = 250
        };

        // Assert
        dto.Success.Should().BeTrue();
        dto.Message.Should().Be("Connection successful");
        dto.ResponseTimeMs.Should().Be(250);
    }

    #endregion

    #region AiProviders Tests

    [Fact(DisplayName = "AiProviders should define all provider constants")]
    public void AiProviders_ShouldDefineAllProviderConstants()
    {
        // Assert
        AiProviders.OpenAI.Should().Be("OpenAI");
        AiProviders.Gemini.Should().Be("Gemini");
        AiProviders.DeepSeek.Should().Be("DeepSeek");
        AiProviders.AzureOpenAI.Should().Be("AzureOpenAI");
        AiProviders.Local.Should().Be("Local");
        AiProviders.Ollama.Should().Be("Ollama");
    }

    [Fact(DisplayName = "AiProviders.All should contain all providers")]
    public void AiProviders_All_ShouldContainAllProviders()
    {
        // Assert
        AiProviders.All.Should().HaveCount(6);
        AiProviders.All.Should().Contain(p => p.Id == "OpenAI");
        AiProviders.All.Should().Contain(p => p.Id == "Gemini");
        AiProviders.All.Should().Contain(p => p.Id == "DeepSeek");
        AiProviders.All.Should().Contain(p => p.Id == "AzureOpenAI");
        AiProviders.All.Should().Contain(p => p.Id == "Local");
        AiProviders.All.Should().Contain(p => p.Id == "Ollama");
    }

    [Fact(DisplayName = "AiProviders OpenAI should require API key")]
    public void AiProviders_OpenAI_ShouldRequireApiKey()
    {
        // Arrange
        var openAi = AiProviders.All.First(p => p.Id == "OpenAI");

        // Assert
        openAi.RequiresApiKey.Should().BeTrue();
        openAi.RequiresAzureConfig.Should().BeFalse();
        openAi.RequiresLocalEndpoint.Should().BeFalse();
    }

    [Fact(DisplayName = "AiProviders AzureOpenAI should require Azure config")]
    public void AiProviders_AzureOpenAI_ShouldRequireAzureConfig()
    {
        // Arrange
        var azure = AiProviders.All.First(p => p.Id == "AzureOpenAI");

        // Assert
        azure.RequiresApiKey.Should().BeTrue();
        azure.RequiresAzureConfig.Should().BeTrue();
    }

    [Fact(DisplayName = "AiProviders Ollama should require local endpoint")]
    public void AiProviders_Ollama_ShouldRequireLocalEndpoint()
    {
        // Arrange
        var ollama = AiProviders.All.First(p => p.Id == "Ollama");

        // Assert
        ollama.RequiresApiKey.Should().BeFalse();
        ollama.RequiresLocalEndpoint.Should().BeTrue();
        ollama.DefaultEndpoint.Should().Contain("localhost:11434");
    }

    [Fact(DisplayName = "AiProviders should have default models for each provider")]
    public void AiProviders_ShouldHaveDefaultModelsForEachProvider()
    {
        // Assert
        foreach (var provider in AiProviders.All)
        {
            provider.DefaultModels.Should().NotBeEmpty($"Provider {provider.Id} should have default models");
        }
    }

    #endregion

    #region AiProviderInfo Tests

    [Fact(DisplayName = "AiProviderInfo should have default values")]
    public void AiProviderInfo_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var info = new AiProviderInfo();

        // Assert
        info.Id.Should().BeEmpty();
        info.Name.Should().BeEmpty();
        info.Description.Should().BeEmpty();
        info.RequiresApiKey.Should().BeFalse();
        info.RequiresAzureConfig.Should().BeFalse();
        info.RequiresLocalEndpoint.Should().BeFalse();
        info.DefaultEndpoint.Should().BeNull();
        info.DefaultModels.Should().BeEmpty();
    }

    #endregion

    #region EmailConfigDto Tests

    [Fact(DisplayName = "EmailConfigDto should have default values")]
    public void EmailConfigDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new EmailConfigDto();

        // Assert
        dto.Id.Should().Be(0);
        dto.Proveedor.Should().Be("SMTP");
        dto.Habilitado.Should().BeFalse();
        dto.SmtpUseSsl.Should().BeFalse();
        dto.FromEmail.Should().BeEmpty();
        dto.FromName.Should().BeEmpty();
        dto.HasSendGridApiKey.Should().BeFalse();
        dto.HasMailgunApiKey.Should().BeFalse();
    }

    [Fact(DisplayName = "EmailConfigDto should store SMTP configuration")]
    public void EmailConfigDto_ShouldStoreSmtpConfiguration()
    {
        // Arrange & Act
        var dto = new EmailConfigDto
        {
            Id = 1,
            Proveedor = "SMTP",
            Habilitado = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpUsername = "user@example.com",
            SmtpUseSsl = true,
            FromEmail = "noreply@example.com",
            FromName = "Ceiba System"
        };

        // Assert
        dto.SmtpHost.Should().Be("smtp.example.com");
        dto.SmtpPort.Should().Be(587);
        dto.SmtpUseSsl.Should().BeTrue();
    }

    [Fact(DisplayName = "EmailConfigDto should track test results")]
    public void EmailConfigDto_ShouldTrackTestResults()
    {
        // Arrange
        var testedAt = DateTime.UtcNow;

        // Act
        var dto = new EmailConfigDto
        {
            LastTestedAt = testedAt,
            LastTestSuccess = true,
            LastTestError = null
        };

        // Assert
        dto.LastTestedAt.Should().Be(testedAt);
        dto.LastTestSuccess.Should().BeTrue();
        dto.LastTestError.Should().BeNull();
    }

    #endregion

    #region EmailConfigUpdateDto Tests

    [Fact(DisplayName = "EmailConfigUpdateDto should have default values")]
    public void EmailConfigUpdateDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new EmailConfigUpdateDto();

        // Assert
        dto.Proveedor.Should().Be("SMTP");
        dto.Habilitado.Should().BeFalse();
        dto.SmtpUseSsl.Should().BeTrue();
        dto.FromEmail.Should().BeEmpty();
        dto.FromName.Should().BeEmpty();
    }

    [Fact(DisplayName = "EmailConfigUpdateDto should store provider-specific settings")]
    public void EmailConfigUpdateDto_ShouldStoreProviderSpecificSettings()
    {
        // Arrange & Act - SendGrid config
        var sendGridDto = new EmailConfigUpdateDto
        {
            Proveedor = "SendGrid",
            SendGridApiKey = "SG.xxxxxxxxxxxx",
            FromEmail = "noreply@example.com"
        };

        // Assert
        sendGridDto.SendGridApiKey.Should().StartWith("SG.");

        // Arrange & Act - Mailgun config
        var mailgunDto = new EmailConfigUpdateDto
        {
            Proveedor = "Mailgun",
            MailgunApiKey = "key-xxxxxxxxxxxx",
            MailgunDomain = "mg.example.com",
            MailgunRegion = "EU"
        };

        // Assert
        mailgunDto.MailgunDomain.Should().Be("mg.example.com");
        mailgunDto.MailgunRegion.Should().Be("EU");
    }

    #endregion

    #region TestEmailConfigDto Tests

    [Fact(DisplayName = "TestEmailConfigDto should have default TestRecipient")]
    public void TestEmailConfigDto_ShouldHaveDefaultTestRecipient()
    {
        // Arrange & Act
        var dto = new TestEmailConfigDto();

        // Assert
        dto.TestRecipient.Should().BeEmpty();
    }

    #endregion

    #region EmailConfigTestResultDto Tests

    [Fact(DisplayName = "EmailConfigTestResultDto should store test results")]
    public void EmailConfigTestResultDto_ShouldStoreTestResults()
    {
        // Arrange
        var testedAt = DateTime.UtcNow;

        // Act
        var dto = new EmailConfigTestResultDto
        {
            Success = false,
            Error = "Connection timeout",
            TestedAt = testedAt
        };

        // Assert
        dto.Success.Should().BeFalse();
        dto.Error.Should().Be("Connection timeout");
        dto.TestedAt.Should().Be(testedAt);
    }

    #endregion

    #region AutomatedReportConfigDto Tests

    [Fact(DisplayName = "AutomatedReportConfigDto should have default values")]
    public void AutomatedReportConfigDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new AutomatedReportConfigDto();

        // Assert
        dto.Id.Should().Be(0);
        dto.Habilitado.Should().BeFalse();
        dto.Destinatarios.Should().BeEmpty();
        dto.RutaSalida.Should().BeEmpty();
        dto.DefaultTemplateId.Should().BeNull();
    }

    [Fact(DisplayName = "AutomatedReportConfigDto compatibility properties should work")]
    public void AutomatedReportConfigDto_CompatibilityProperties_ShouldWork()
    {
        // Arrange
        var dto = new AutomatedReportConfigDto();
        var time = new TimeSpan(6, 30, 0);

        // Act
        dto.GenerationTime = time;
        dto.Enabled = true;

        // Assert
        dto.HoraGeneracion.Should().Be(time);
        dto.GenerationTime.Should().Be(time);
        dto.Habilitado.Should().BeTrue();
        dto.Enabled.Should().BeTrue();
    }

    [Fact(DisplayName = "AutomatedReportConfigDto SetRecipients should work")]
    public void AutomatedReportConfigDto_SetRecipients_ShouldWork()
    {
        // Arrange
        var dto = new AutomatedReportConfigDto();
        var recipients = new[] { "admin@example.com", "supervisor@example.com" };

        // Act
        dto.SetRecipients(recipients);

        // Assert
        dto.Destinatarios.Should().HaveCount(2);
        dto.Recipients.Should().HaveCount(2);
        dto.Recipients.Should().Contain("admin@example.com");
    }

    #endregion

    #region AutomatedReportListDto Tests

    [Fact(DisplayName = "AutomatedReportListDto should have default values")]
    public void AutomatedReportListDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new AutomatedReportListDto();

        // Assert
        dto.Id.Should().Be(0);
        dto.Enviado.Should().BeFalse();
        dto.TieneError.Should().BeFalse();
        dto.FechaEnvio.Should().BeNull();
        dto.NombreModelo.Should().BeNull();
    }

    #endregion

    #region AutomatedReportDetailDto Tests

    [Fact(DisplayName = "AutomatedReportDetailDto should have default values")]
    public void AutomatedReportDetailDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new AutomatedReportDetailDto();

        // Assert
        dto.ContenidoMarkdown.Should().BeEmpty();
        dto.ContenidoWordPath.Should().BeNull();
        dto.Estadisticas.Should().NotBeNull();
        dto.ErrorMensaje.Should().BeNull();
    }

    #endregion

    #region ReportStatisticsDto Tests

    [Fact(DisplayName = "ReportStatisticsDto should have default values")]
    public void ReportStatisticsDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new ReportStatisticsDto();

        // Assert
        dto.TotalReportes.Should().Be(0);
        dto.ReportesEntregados.Should().Be(0);
        dto.ReportesBorrador.Should().Be(0);
        dto.PorSexo.Should().NotBeNull().And.BeEmpty();
        dto.PorRangoEdad.Should().NotBeNull().And.BeEmpty();
        dto.PorDelito.Should().NotBeNull().And.BeEmpty();
        dto.PorZona.Should().NotBeNull().And.BeEmpty();
        dto.DelitoMasFrecuente.Should().BeNull();
        dto.ZonaMasActiva.Should().BeNull();
    }

    [Fact(DisplayName = "ReportStatisticsDto should store complete statistics")]
    public void ReportStatisticsDto_ShouldStoreCompleteStatistics()
    {
        // Arrange & Act
        var dto = new ReportStatisticsDto
        {
            TotalReportes = 100,
            ReportesEntregados = 80,
            ReportesBorrador = 20,
            PorSexo = new Dictionary<string, int> { { "Masculino", 60 }, { "Femenino", 40 } },
            PorDelito = new Dictionary<string, int> { { "Robo", 30 }, { "Vandalismo", 20 } },
            DelitoMasFrecuente = "Robo",
            TotalLgbtttiq = 5,
            TotalMigrantes = 10,
            ConTraslado = 15,
            SinTraslado = 70,
            TrasladoNoAplica = 15
        };

        // Assert
        dto.TotalReportes.Should().Be(100);
        dto.PorSexo.Should().HaveCount(2);
        dto.DelitoMasFrecuente.Should().Be("Robo");
        dto.ConTraslado.Should().Be(15);
    }

    #endregion

    #region ReportTemplateDto Tests

    [Fact(DisplayName = "ReportTemplateDto should have default Activo as true")]
    public void ReportTemplateDto_ShouldHaveDefaultActivoAsTrue()
    {
        // Arrange & Act
        var dto = new ReportTemplateDto();

        // Assert
        dto.Activo.Should().BeTrue();
        dto.EsDefault.Should().BeFalse();
    }

    #endregion

    #region GenerateReportRequestDto Tests

    [Fact(DisplayName = "GenerateReportRequestDto should have default values")]
    public void GenerateReportRequestDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new GenerateReportRequestDto();

        // Assert
        dto.ModeloReporteId.Should().BeNull();
        dto.EnviarEmail.Should().BeFalse();
        dto.EmailDestinatarios.Should().BeNull();
    }

    #endregion

    #region NarrativeRequestDto Tests

    [Fact(DisplayName = "NarrativeRequestDto should have initialized collections")]
    public void NarrativeRequestDto_ShouldHaveInitializedCollections()
    {
        // Arrange & Act
        var dto = new NarrativeRequestDto();

        // Assert
        dto.Statistics.Should().NotBeNull();
        dto.Incidents.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region NarrativeResponseDto Tests

    [Fact(DisplayName = "NarrativeResponseDto should have default values")]
    public void NarrativeResponseDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new NarrativeResponseDto();

        // Assert
        dto.Narrativa.Should().BeEmpty();
        dto.Success.Should().BeFalse();
        dto.Error.Should().BeNull();
        dto.TokensUsed.Should().Be(0);
    }

    #endregion

    #region SendEmailRequestDto Tests

    [Fact(DisplayName = "SendEmailRequestDto should have initialized collections")]
    public void SendEmailRequestDto_ShouldHaveInitializedCollections()
    {
        // Arrange & Act
        var dto = new SendEmailRequestDto();

        // Assert
        dto.Recipients.Should().NotBeNull().And.BeEmpty();
        dto.Subject.Should().BeEmpty();
        dto.BodyHtml.Should().BeEmpty();
        dto.Attachments.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region EmailAttachmentDto Tests

    [Fact(DisplayName = "EmailAttachmentDto should have default values")]
    public void EmailAttachmentDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new EmailAttachmentDto();

        // Assert
        dto.FileName.Should().BeEmpty();
        dto.Content.Should().BeEmpty();
        dto.ContentType.Should().Be("application/octet-stream");
    }

    [Fact(DisplayName = "EmailAttachmentDto should store attachment data")]
    public void EmailAttachmentDto_ShouldStoreAttachmentData()
    {
        // Arrange
        var content = new byte[] { 0x50, 0x44, 0x46 }; // PDF magic bytes

        // Act
        var dto = new EmailAttachmentDto
        {
            FileName = "report.pdf",
            Content = content,
            ContentType = "application/pdf"
        };

        // Assert
        dto.FileName.Should().Be("report.pdf");
        dto.Content.Should().HaveCount(3);
        dto.ContentType.Should().Be("application/pdf");
    }

    #endregion

    #region SendEmailResultDto Tests

    [Fact(DisplayName = "SendEmailResultDto should have default values")]
    public void SendEmailResultDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new SendEmailResultDto();

        // Assert
        dto.Success.Should().BeFalse();
        dto.Error.Should().BeNull();
        dto.SentAt.Should().BeNull();
    }

    #endregion
}
