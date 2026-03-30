using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WandEnhancer.Models;

namespace WandEnhancer.Core
{
    public static class EnhancerConfig
    {
        public class ResolveContext
        {
            public string Placeholder { get; set; }
            public Func<string, string> Handler { get; set; }
        }

        public class PatchEntry
        {
            public Regex Target { get; set; }
            public string Patch { get; set; }
            public string Name { get; set; }
            public bool Applied { get; set; }
            public bool SingleMatch { get; set; } = true;
            public ResolveContext Resolver { get; set; }
        }

        public static Dictionary<EPatchType, PatchEntry[]> GetInstance()
        {
            return new Dictionary<EPatchType, PatchEntry[]>()
            {
                {
                    EPatchType.ActivatePro,
                    new[]
                    {
                        new PatchEntry
                        {
                            Resolver = new ResolveContext
                            {
                                Handler = (targetFunction) =>
                                {
                                    var fetchMatch = Regex.Match(targetFunction, @"return\s+this\.#(\w+)\.fetch");
                                    return fetchMatch.Success ? fetchMatch.Groups[1].Value : null;
                                },
                                Placeholder = "<service_name>"
                            },
                            Name = "getUserAccount",
                            Target = new Regex(@"getUserAccount\(\)\{.*?return\s+this\.#\w+\.fetch\(\{.*?\}\)\}",
                                RegexOptions.Singleline),
                            Patch =
                                "getUserAccount(){return this.#<service_name>.fetch({endpoint:\"/v3/account\",method:\"GET\",name:\"/v3/account\",collectMetrics:0}).then(response=>{response.subscription={period:\"yearly\",state:\"active\"};return response;})}"
                        },
                        new PatchEntry
                        {
                            Resolver = new ResolveContext
                            {
                                Handler = (targetFunction) =>
                                {
                                    var match = Regex.Match(targetFunction, @"return\s+this\.#(\w+)\.post");
                                    return match.Success ? match.Groups[1].Value : null;
                                },
                                Placeholder = "<service_name>"
                            },
                            Name = "setAccountWandBrandExperience",
                            Target = new Regex(
                                @"setAccountWandBrandExperience\(\){.*?return\s+this\.#\w+\.post\(""/v3/account/brand_experience_wand""\)}",
                                RegexOptions.Singleline),
                            Patch =
                                "setAccountWandBrandExperience(){return this.#<service_name>.post(\"/v3/account/brand_experience_wand\").then(response=>{response.subscription={period:\"yearly\",state:\"active\"};return response;})}"
                        }
                    }
                },
                {
                    EPatchType.DisableUpdates,
                    new[]
                    {
                        new PatchEntry
                        {
                            Target = new Regex(@"registerHandler\(""ACTION_CHECK_FOR_UPDATE"".*?\)\)\)\)",
                                RegexOptions.Singleline),
                            Patch = "registerHandler(\"ACTION_CHECK_FOR_UPDATE\",(e=>expectUpdateFeedUrl(e,(e=>null)))"
                        }
                    }
                },
                {
                    EPatchType.DevToolsOnF12,
                    new[]
                    {
                        new PatchEntry
                        {
                            Resolver = new ResolveContext
                            {
                                Handler = (matchContent) => {
                                    var match = Regex.Match(matchContent, @"this\.#(\w+)\(""ACTION_OPEN_DEV_TOOLS""\)");
                                    return match.Success ? match.Groups[1].Value : null;
                                },
                                Placeholder = "<dispatch_method>"
                            },
                            Target = new Regex(@"document\.addEventListener\(""keydown"",\s*\((?<arg>\w+)\s*=>\s*\{[^}]*?""ACTION_OPEN_DEV_TOOLS""[^}]*?\}\)\)", RegexOptions.Singleline),
                            Patch = "document.addEventListener(\"keydown\",(${arg}=>{\"F12\"!==${arg}.key||this.#<dispatch_method>(\"ACTION_OPEN_DEV_TOOLS\")}))"
                        }
                    }
                }
            };
        }
    }
}