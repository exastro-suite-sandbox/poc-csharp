#!/bin/bash

cd "$(dirname "${BASH_SOURCE[0]}")"
cargo build --locked --package=minijinja-cabi --release
cargo expand --locked --package=minijinja-cabi > ./minijinja_cabi.rs
cargo build --locked --release
