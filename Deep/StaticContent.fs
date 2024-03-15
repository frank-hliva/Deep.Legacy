namespace Deep

open System.IO
open System.Web
open Deep.IO

type StaticContentOptions = { Directory : string; BufferSize : int }

type StaticContentConfig(config : Config) =
    interface IConfigSection
    member c.GetOptions() =
        let options = config.SelectAs<StaticContentOptions>("StaticContent")
        { options with Directory = options.Directory |> Path.map }

type StaticContent(staticContentOptions : StaticContentOptions) =

    let rec tryExists (path : string) =
        if File.Exists path then Some path
        else
            if path.Contains("?") then
                path |> Url.removeQueryString |> tryExists
            else None

    interface IListener with

        member l.Listen (request : Request) (response : Response) (kernel : IKernel) (e : exn option) = async {
            if request.RawUrl = "/" then return ListenerResult.Next
            else
                let path = Path.join([staticContentOptions.Directory; request.RawUrl])
                match path |> tryExists with
                | Some path ->
                    do! File.send(path, response, {
                        BufferSize = staticContentOptions.BufferSize
                        ContentType = null
                    })
                    return ListenerResult.End
                | _ ->
                    return ListenerResult.Next }

    new(config : StaticContentConfig) =
        StaticContent(config.GetOptions())