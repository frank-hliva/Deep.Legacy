namespace App

open Deep

type ErrorController() =
    inherit FrontendController()

    member c.Page403(reply : Reply) =
        c.Title <- "Page 403"
        reply.StatusCode <- 403
        reply.View()

    member c.Page404(reply : Reply) =
        c.Title <- "Page 404"
        reply.StatusCode <- 404
        reply.View()

    member c.Page500(reply : Reply) =
        c.Title <- "Page 500"
        reply.StatusCode <- 500
        reply.View()