package main

import (
	"fmt"
	"helpers"
)

func main() {
	vs, vcs, err := helpers.FetchAndProcess()
	if err != nil {
		fmt.Println("Error:", err)
		return
	}

	fmt.Println("VS:", vs)
	fmt.Println("VCs:")
	for i, v := range vcs {
		fmt.Printf("  [%d] %s\n", i, v)
	}
}
