package main

import (
	"bufio"
	"fmt"
	"os"
	"os/exec"
)

func main() {
	for _, arg := range os.Args[1:] {
		fmt.Printf("file: %s\n\n", arg)
		cmd := exec.Command("pyrevit", "revits", "fileinfo", arg)
		stdout, _ := cmd.StdoutPipe()
		cmd.Start()

		scanner := bufio.NewScanner(stdout)
		scanner.Split(bufio.ScanLines)
		for scanner.Scan() {
			m := scanner.Text()
			fmt.Println(m)
		}
		cmd.Wait()
	}

	// wait for user input to keep window open
	reader := bufio.NewReader(os.Stdin)
	reader.ReadByte()
}
