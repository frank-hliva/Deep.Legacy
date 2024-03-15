namespace Deep

open System
open System.IO
open Deep.Routing
open Deep.IO

type ViewData = Map<string, obj>

[<AllowNullLiteral>]
type IView =
    abstract Render : routeParams : RouteParams * path : string option * viewData : ViewData option -> string

type ViewOptions = { Extension : string; Directory : string }

type ViewConfig(config : Config) =
    interface IConfigSection
    member c.GetOptions() =
        let options = config.SelectAs<ViewOptions>("View")
        { options with Directory = options.Directory |> Path.map }

type ViewPathFinder(viewOptions : ViewOptions) =

    member f.TryFind (parameters : RouteParams, path) =
        let fn = Path.ChangeExtension(path, viewOptions.Extension)
        [
            Path.join([viewOptions.Directory; parameters.["Controller"]; fn])
            Path.join([viewOptions.Directory; fn])
            Path.join([viewOptions.Directory; "Shared"; fn])
            fn
        ] |> List.tryFind(File.Exists)

    member f.TryFind (parameters : RouteParams) =
        f.TryFind(parameters, parameters.["Action"])

    new(viewConfig : ViewConfig) = ViewPathFinder(viewConfig.GetOptions())