package fixtures

import _ "embed"

// SamplePDF provides a valid PDF byte slice for test suites.
//
//go:embed sample.pdf
var SamplePDF []byte
