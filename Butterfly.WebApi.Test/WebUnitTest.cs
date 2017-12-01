﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Butterfly.Util;
using Butterfly.WebApi.EmbedIO;
using System.Net;

namespace Butterfly.WebApi.Test {
    [TestClass]
    public class WebUnitTest {
        [TestMethod]
        public async Task RedHttpServerWeb() {
            var redHttpServer = new RedHttpServerNet45.RedHttpServer(8080);
            using (var webApiServer = new Butterfly.WebApi.RedHttpServer.RedHttpServerWebApiServer(redHttpServer)) {
                await this.TestWeb(webApiServer, "http://localhost:8080/", () => {
                    redHttpServer.Start();
                });
            }
        }

        [TestMethod]
        public async Task EmbedIOWeb() {
            using (var embedIOWebServer = new Unosquare.Labs.EmbedIO.WebServer("http://localhost:8080/"))
            using (var webApiServer = new EmbedIOWebApiServer(embedIOWebServer)) {
                await this.TestWeb(webApiServer, "http://localhost:8080/", () => {
                    embedIOWebServer.RunAsync();
                });
            }
        }

        public async Task TestWeb(IWebApiServer webServer, string url, Action start) {
            // Add routes
            webServer.OnGet("/test-get", async (req, res) => {
                await res.WriteAsJsonAsync("test-get-response");
            });
            webServer.OnPost("/test-post", async (req, res) => {
                var text = await req.ParseAsJsonAsync<string>();
                Assert.AreEqual("test-post-request", text);

                await res.WriteAsJsonAsync("test-post-response");
            });
            webServer.Start();

            // Start the underlying server
            start();

            // Test GET request
            using (WebClient webClient = new WebClient()) {
                string json = await webClient.DownloadStringTaskAsync(new Uri($"{url}test-get"));
                string text = JsonUtil.Deserialize<string>(json);
                Assert.AreEqual("test-get-response", text);
            }

            // Test POST request
            using (WebClient webClient = new WebClient()) {
                string uploadJson = JsonUtil.Serialize("test-post-request");
                string downloadJson = await webClient.UploadStringTaskAsync(new Uri($"{url}test-post"), uploadJson);
                string downloadText = JsonUtil.Deserialize<string>(downloadJson);
                Assert.AreEqual("test-post-response", downloadText);
            }
        }
    }

}
