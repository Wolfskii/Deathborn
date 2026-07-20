package worldmap

import (
	_ "embed"
	"encoding/binary"
	"fmt"
)

//go:embed swarovia_mainland_elevation.bin
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

	// North/south only on the stair tread cells (or straight onto landing from south).
	if dx == 0 && dy == -1 && te == fe+1 {
		if g.rampAt(tx, ty, tw, th) != 0 && fx == tx && fy == ty+1 {
			return true
		}
		rx1, ry1, _, ok1 := g.rampTreadCellID(fx, fy, tw, th)
		rx2, ry2, _, ok2 := g.rampTreadCellID(tx, ty, tw, th)
		if ok1 && ok2 && rx1 == rx2 && ry1 == ry2 {
			return true
		}
	}
	if dx == 0 && dy == 1 && fe == te+1 {
		if g.rampAt(fx, fy, tw, th) != 0 && tx == fx && ty == fy+1 {
			return true
		}
		rx1, ry1, _, ok1 := g.rampTreadCellID(fx, fy, tw, th)
		rx2, ry2, _, ok2 := g.rampTreadCellID(tx, ty, tw, th)
		if ok1 && ok2 && rx1 == rx2 && ry1 == ry2 {
			return true
		}
	}

	// Diagonal step only when both tiles are the stair sprite cells (elevation rules still apply).
	if dx != 0 && dy != 0 {
		rx1, ry1, _, ok1 := g.rampTreadCellID(fx, fy, tw, th)
		rx2, ry2, _, ok2 := g.rampTreadCellID(tx, ty, tw, th)
		if ok1 && ok2 && rx1 == rx2 && ry1 == ry2 {
			if (fx == rx1 && fy == ry1) || (fx == rx1 && fy == ry1-1) {
				if (tx == rx1 && ty == ry1) || (tx == rx1 && ty == ry1-1) {
					return true
				}
			}
		}
	}
	return false
}

// rampTreadCellID maps a tile to its stair only when it is a tread sprite cell (landing or top).
func (g *elevationGrid) rampTreadCellID(tx, ty, tw, th int) (landingX, landingY, kind int, ok bool) {
	for ry := 0; ry < th; ry++ {
		for rx := 0; rx < tw; rx++ {
			ramp := int(g.rampAt(rx, ry, tw, th))
			if ramp == 0 {
				continue
			}
			if tx == rx && (ty == ry || ty == ry-1) {
				return rx, ry, ramp, true
			}
		}
	}
	return 0, 0, 0, false
}

// rampEngagedAtWorld is true on the green tread band or valid stair entry/approach tiles.
func (g *elevationGrid) rampEngagedAtWorld(wx, wy, tileSize float64, tw, th int) (landingX, landingY, kind int, ok bool) {
	if rx, ry, k, hit := g.rampTreadAtWorld(wx, wy, tileSize, tw, th); hit {
		return rx, ry, k, true
	}

	tx := int(wx / tileSize)
	ty := int(wy / tileSize)
	lx := (wx - float64(tx)*tileSize) / tileSize

	for ry := 0; ry < th; ry++ {
		for rx := 0; rx < tw; rx++ {
			ramp := int(g.rampAt(rx, ry, tw, th))
			if ramp == 0 {
				continue
			}
			if ramp == 1 {
				if tx == rx-1 && ty == ry {
					return rx, ry, 1, true
				}
				if tx == rx && ty == ry+1 && ry+1 < th {
					return rx, ry, 1, true
				}
				if tx == rx && ty == ry && lx < 0.50 {
					return rx, ry, 1, true
				}
				if tx == rx && ty == ry-1 && ry > 0 && lx <= 0.50 {
					return rx, ry, 1, true
				}
			} else {
				lxr := lx
				if tx == rx {
					lxr = 1 - (wx-float64(tx)*tileSize)/tileSize
				}
				if tx == rx+1 && ty == ry {
					return rx, ry, 2, true
				}
				if tx == rx && ty == ry+1 && ry+1 < th {
					return rx, ry, 2, true
				}
				if tx == rx && ty == ry && lxr < 0.50 {
					return rx, ry, 2, true
				}
				if tx == rx && ty == ry-1 && ry > 0 && lxr <= 0.50 {
					return rx, ry, 2, true
				}
			}
		}
	}
	return 0, 0, 0, false
}

// rampTreadAtWorld checks world position against the green tread band inside the stair sprites.
func (g *elevationGrid) rampTreadAtWorld(wx, wy, tileSize float64, tw, th int) (landingX, landingY, kind int, ok bool) {
	for ry := 0; ry < th; ry++ {
		for rx := 0; rx < tw; rx++ {
			ramp := int(g.rampAt(rx, ry, tw, th))
			if ramp == 0 {
				continue
			}
			if ramp == 1 && g.onLeftRampTread(wx, wy, tileSize, rx, ry) {
				return rx, ry, 1, true
			}
			if ramp == 2 && g.onRightRampTread(wx, wy, tileSize, rx, ry) {
				return rx, ry, 2, true
			}
		}
	}
	return 0, 0, 0, false
}

// onLeftRampTread matches the green diagonal band in pieces 29 (bottom) and 25 (top).
func (g *elevationGrid) onLeftRampTread(wx, wy, tileSize float64, rx, ry int) bool {
	tx := int(wx / tileSize)
	ty := int(wy / tileSize)
	lx := (wx - float64(tx)*tileSize) / tileSize
	ly := (wy - float64(ty)*tileSize) / tileSize

	if tx == rx && ty == ry {
		if lx < 0.42 {
			return false
		}
		return rampBandHit(lx, ly, 0.46, 0.56, 0.93, 0.14, 0.11)
	}
	if tx == rx && ty == ry-1 && ry > 0 {
		if lx > 0.50 {
			return false
		}
		return rampBandHit(lx, ly, 0.10, 0.66, 0.48, 0.34, 0.10)
	}
	return false
}

// onRightRampTread matches pieces 32 (bottom) and 28 (top).
func (g *elevationGrid) onRightRampTread(wx, wy, tileSize float64, rx, ry int) bool {
	tx := int(wx / tileSize)
	ty := int(wy / tileSize)
	lx := 1 - (wx-float64(tx)*tileSize)/tileSize
	ly := (wy - float64(ty)*tileSize) / tileSize

	if tx == rx && ty == ry {
		if lx < 0.42 {
			return false
		}
		return rampBandHit(lx, ly, 0.46, 0.56, 0.93, 0.14, 0.11)
	}
	if tx == rx && ty == ry-1 && ry > 0 {
		if lx > 0.50 {
			return false
		}
		return rampBandHit(lx, ly, 0.10, 0.66, 0.48, 0.34, 0.10)
	}
	return false
}

func rampBandHit(lx, ly, ax, ay, bx, by, halfW float64) bool {
	dx := bx - ax
	dy := by - ay
	len2 := dx*dx + dy*dy
	if len2 < 0.0001 {
		return false
	}
	t := ((lx-ax)*dx + (ly-ay)*dy) / len2
	if t < 0 {
		t = 0
	} else if t > 1 {
		t = 1
	}
	px := ax + t*dx
	py := ay + t*dy
	ddx := lx - px
	ddy := ly - py
	return ddx*ddx+ddy*ddy <= halfW*halfW
}

// rampSlopeAtWorld returns stair slope when feet are on the tread band or a valid entry tile.
func (g *elevationGrid) rampSlopeAtWorld(wx, wy, tileSize float64, tw, th int) (ok bool, dyPerDx float64) {
	_, _, kind, hit := g.rampEngagedAtWorld(wx, wy, tileSize, tw, th)
	if !hit {
		return false, 0
	}
	if kind == 1 {
		return true, -1
	}
	return true, 1
}
