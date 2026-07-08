package worldmap

import (
	_ "embed"
	"encoding/binary"
	"fmt"
)

//go:embed realik_elevation.bin
var elevationData []byte

type elevationGrid struct {
	elevation []int8
	ramps     []uint8
}

func loadElevation(tw, th int) (*elevationGrid, error) {
	data := elevationData
	if len(data) < 9 || string(data[:4]) != "ELEV" {
		return nil, fmt.Errorf("worldmap: invalid elevation header")
	}
	version := data[4]
	if version != 1 && version != 2 {
		return nil, fmt.Errorf("worldmap: unsupported elevation version %d", version)
	}

	etw := int(binary.LittleEndian.Uint16(data[5:7]))
	eth := int(binary.LittleEndian.Uint16(data[7:9]))
	if etw != tw || eth != th {
		return nil, fmt.Errorf("worldmap: elevation size mismatch %dx%d vs %dx%d", etw, eth, tw, th)
	}

	expected := 9 + tw*th
	if version == 2 {
		expected = 9 + tw*th*2
	}
	if len(data) < expected {
		return nil, fmt.Errorf("worldmap: elevation truncated (%d < %d)", len(data), expected)
	}

	n := tw * th
	elev := make([]int8, n)
	for i := 0; i < n; i++ {
		elev[i] = int8(data[9+i])
	}

	ramps := make([]uint8, n)
	if version == 2 {
		rampOff := 9 + n
		for i := 0; i < n; i++ {
			ramps[i] = data[rampOff+i]
		}
	}

	return &elevationGrid{elevation: elev, ramps: ramps}, nil
}

func (g *elevationGrid) at(tx, ty, tw, th int) int8 {
	if tx < 0 || ty < 0 || tx >= tw || ty >= th {
		return -1
	}
	return g.elevation[ty*tw+tx]
}

func (g *elevationGrid) rampAt(tx, ty, tw, th int) uint8 {
	if tx < 0 || ty < 0 || tx >= tw || ty >= th {
		return 0
	}
	return g.ramps[ty*tw+tx]
}

func (g *elevationGrid) canStep(fx, fy, tx, ty, tw, th int) bool {
	fe := g.at(fx, fy, tw, th)
	te := g.at(tx, ty, tw, th)
	if fe < 0 || te < 0 {
		return false
	}
	if fe == te {
		return true
	}
	diff := int(fe) - int(te)
	if diff < 0 {
		diff = -diff
	}
	if diff != 1 {
		return false
	}

	dx := tx - fx
	dy := ty - fy

	if dx == 1 && dy == 0 && te == fe+1 && g.rampAt(fx, fy+1, tw, th) == 1 {
		return true
	}
	if dx == -1 && dy == 0 && fe == te+1 && g.rampAt(tx, ty+1, tw, th) == 1 {
		return true
	}
	if dx == -1 && dy == 0 && te == fe+1 && g.rampAt(fx, fy+1, tw, th) == 2 {
		return true
	}
	if dx == 1 && dy == 0 && fe == te+1 && g.rampAt(tx, ty+1, tw, th) == 2 {
		return true
	}
	return false
}
