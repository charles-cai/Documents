﻿namespace FSharpWeb.Core

open System
open System.Collections.Generic
open System.Linq
open System.Web
open System.Web.Mvc
open System.Web.Routing

/// F# record that can be used for creating route information
type Route = 
  { controller : string
    action : string }

type RouteWithId = 
  { controller : string
    action : string 
    id : int }

/// Represents the application and registers routes
type Global() =
  inherit System.Web.HttpApplication() 

  static member RegisterRoutes(routes:RouteCollection) =
    routes.IgnoreRoute("{resource}.axd/{*pathInfo}")
    routes.MapRoute
      ( "Default", "{action}",
        { Route.controller = "Main"; action = "Index" }) |> ignore
    routes.MapRoute
      ( "Default1", "Vote/{id}",
        { RouteWithId.controller = "Main"; action = "Vote"; id = -1 }) |> ignore

  member x.Start() =
    AreaRegistration.RegisterAllAreas()
    Global.RegisterRoutes(RouteTable.Routes)