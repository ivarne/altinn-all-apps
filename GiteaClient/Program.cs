using NSwag;
using NSwag.CodeGeneration.CSharp;

var client = new HttpClient();
var swagger = await client.GetStringAsync("https://altinn.studio/repos/swagger.v1.json");

var document = await OpenApiDocument.FromJsonAsync(swagger);

var settings = new CSharpClientGeneratorSettings
{
    ClassName = "GiteaClient",
    CSharpGeneratorSettings =
    {
        Namespace = "allApps.Runner.Clients",
        GenerateNullableReferenceTypes = true,
        JsonLibrary = NJsonSchema.CodeGeneration.CSharp.CSharpJsonLibrary.SystemTextJson,
        GenerateOptionalPropertiesAsNullable = true,
    },
    UseBaseUrl = false,
    InjectHttpClient = true,
    GenerateOptionalParameters = true,
};

var generator = new CSharpClientGenerator(document, settings);
var code = generator.GenerateFile();

await File.WriteAllTextAsync("../Runner/GiteaClient.cs", code);