namespace Deep.View.DotLiquid

open Deep
open System.IO
open System.Text
open DotLiquid
open System.Collections
open System.Collections.Generic
open Deep.Routing
open Deep.Collections
open Microsoft.FSharp.Reflection
open System.Reflection

type View(viewConfig : ViewConfig, viewPathFinder : ViewPathFinder) =

    let valueObjectConverter = ValueObjectConverter(Hash.FromDictionary >> box)

    let toRenderParameters (viewData : ViewData option) =
        match viewData with
        | Some viewData -> viewData
        | _ -> Map.empty
        |> Map.toDict
        |> valueObjectConverter.ConvertToDict :?> Hash

    let readFromFile (routeParams : RouteParams) (path : string option) =
        path
        |> function
        | Some path -> viewPathFinder.TryFind(routeParams, path)
        | _ -> viewPathFinder.TryFind(routeParams)
        |> function
        | Some path -> File.ReadAllText(path, Encoding.UTF8)
        | _ -> failwith "Template file not fonud"

    let localFileSystem (routeParams : RouteParams) =
        { new DotLiquid.FileSystems.IFileSystem with
            member this.ReadTemplateFile(context, name) =
                name.Trim([|'\"';'\''|]).Trim()
                |> Some |> readFromFile routeParams }

    interface IView with
        override v.Render(routeParams, path, viewData) =
            let text = path |> readFromFile routeParams
            let o = new obj()
            lock o (fun () -> Template.FileSystem <- localFileSystem routeParams)
            viewData |> toRenderParameters |> Template.Parse(text).Render

    new(viewConfig : ViewConfig) =
        View(viewConfig, new ViewPathFinder(viewConfig))