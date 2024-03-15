namespace App

open Deep
open System.Linq

type HomeController(reply : Reply) =
    inherit FrontendController()

    member c.Index(flashMessages : FlashMessages) = async {
        c.Title <- "Index"
        do! flashMessages.Send("Flash message")
        reply.View ["Name" => "world"] }

    member c.LearnMore(sessions : ISessionManager) = async {
        c.Title <- "Learn more"
        let! counter = sessions.GetItemOrDefault<int>("counter")
        do! sessions.SetItem("counter", counter + 1)
        let! counter = sessions.GetItem<int>("counter")
        reply.ViewData.["counter"] <- counter
        reply.View() }