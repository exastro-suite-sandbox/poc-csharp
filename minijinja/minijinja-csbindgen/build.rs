fn main() {
    csbindgen::Builder::default()
        .input_extern_file("minijinja_cabi.rs")
        .csharp_dll_name("minijinja_cabi")
        .generate_csharp_file("../NativeMethods.g.cs")
        .unwrap();
}