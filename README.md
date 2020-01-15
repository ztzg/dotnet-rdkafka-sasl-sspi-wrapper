# RdkafkaSaslSspiWrapper

This is a close-to-minimal P/Invoke wrapper around
`rdkafka_sasl_sspi`, a trimmed version of `librdkafka` which exposes
its SASL GSSAPI implementation on top of Windows's SSPI.

The native library can be obtained by compiling the
`RT-46545-zookeeper-net-sasl-WIP` branch of this repository:

https://github.com/ztzg/librdkafka
