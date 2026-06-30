package game

type healOverTime struct {
	ability   string
	interval  float64
	accum     float64
	perTick   int
	ticksLeft int
}
