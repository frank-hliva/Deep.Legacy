module Deep.Imaging.Sizing

open System
open System.Windows

type Fit = 
| Inside = 0
| Outside = 1
| Fill = 2

type Size with
    member s.FitToArea(area : Size, change : Fit) =
        match change with
        | Fit.Fill -> area
        | Fit.Inside | Fit.Outside ->
            let op = if change = Fit.Inside then (<) else (>)
            let w, h = area.Width / s.Width, area.Height / s.Height
            let ratio = if op h w then h else w
            new Size(s.Width * ratio, s.Height * ratio)
        | _ -> raise(new InvalidOperationException())
    member s.FitToArea(width, height, change) = s.FitToArea(new Size(width, height), change)
    member s.FitToArea(area : Size) = s.FitToArea(area, Fit.Inside)
    member s.FitToArea(width, height) = s.FitToArea(width, height, Fit.Inside)