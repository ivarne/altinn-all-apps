using allApps.Runner;
using allApps.Runner.Clients;

var apiKey = "37a912bc51ab32bfcaa500db6cc00ab99263e42c";

Directory.SetCurrentDirectory("../apps");

var giteaClient = GiteaClientFactory.CreateClient(apiKey, useLoggingHandler: false);
Directory.CreateDirectory("main");
var baseDir = new DirectoryInfo("main");
var checkoutAllReposStarter = new CheckoutAllReposStarter(giteaClient, baseDir, apiKey);
await checkoutAllReposStarter.CloneOrPullAllAppReposMaster();

var checkoutDeployedReposJobStarter = new CheckoutDeployedReposJobStarter(useLoggingHandler: false);
await checkoutDeployedReposJobStarter.RunAll();
