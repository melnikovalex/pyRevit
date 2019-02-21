package main

import "github.com/posener/complete"

func main() {

	pyrevit := complete.Command{
		Sub: complete.Commands{
			"attach": complete.Command{
				// Flags: complete.Flags{
				// 	"-cpus": complete.PredictAnything,
				// },
			},
			"attached": complete.Command{},
			"blog":     complete.Command{},
			"caches":   complete.Command{},
			"cli":      complete.Command{},
			"clone":    complete.Command{},
			"clones": complete.Command{
				Sub: complete.Commands{
					"info":        complete.Command{},
					"open":        complete.Command{},
					"add":         complete.Command{},
					"forget":      complete.Command{},
					"rename":      complete.Command{},
					"delete":      complete.Command{},
					"branch":      complete.Command{},
					"version":     complete.Command{},
					"commit":      complete.Command{},
					"origin":      complete.Command{},
					"update":      complete.Command{},
					"deployments": complete.Command{},
					"engines":     complete.Command{},
				},
			},
			"config":  complete.Command{},
			"configs": complete.Command{},
			"detach":  complete.Command{},
			"docs":    complete.Command{},
			"env": complete.Command{
				Flags: complete.Flags{
					"--json": complete.PredictAnything,
				},
			},
			"extend":    complete.Command{},
			"extension": complete.Command{},
			"help":      complete.Command{},
			"init":      complete.Command{},
			"releases": complete.Command{
				Sub: complete.Commands{
					"open": complete.Command{},
					"download": complete.Command{
						Sub: complete.Commands{
							"installer": complete.Command{},
							"archive":   complete.Command{},
						},
					},
				},
			},
			"revits":  complete.Command{},
			"run":     complete.Command{},
			"source":  complete.Command{},
			"support": complete.Command{},
			"switch":  complete.Command{},
			"youtube": complete.Command{},
		},

		Flags: complete.Flags{
			"-V":        complete.PredictNothing,
			"--version": complete.PredictNothing,
			"--h":       complete.PredictNothing,
			"--help":    complete.PredictNothing,
		},

		GlobalFlags: complete.Flags{
			"--help":    complete.PredictNothing,
			"--verbose": complete.PredictNothing,
			"--debug":   complete.PredictNothing,
		},
	}

	complete.New("pyrevit", pyrevit).Run()
}
