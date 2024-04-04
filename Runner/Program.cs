using allApps.Runner;
using allApps.Runner.Clients;

var apiKey = "37a912bc51ab32bfcaa500db6cc00ab99263e42c";

// CheckoutDeployedReposJobStarter starter = new(useLoggingHandler: false);
// Directory.SetCurrentDirectory("../apps");
// await starter.RunAll();

var giteaClient = GiteaClientFactory.CreateClient(apiKey, useLoggingHandler: false);
var checkoutAllReposStarter = new CheckoutAllReposStarter(giteaClient);
Directory.CreateDirectory("../apps/main");
var baseDir = new DirectoryInfo("../apps/main");
await checkoutAllReposStarter.CloneOrPullAllAppReposMaster(baseDir);
