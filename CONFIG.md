# Configuration and Command Line Options

## Command Line Options

'azbridge' can be run in a "local" or "remote" mode. The "local" mode binds a
local listener address or socket to a relay name. The "remote" mode binds a relay 
name to a remote listener address.

Since azbridge helps with scenarios not dissimilar to SSH tunnels, albeit without 
requiring peer-to-peer connectivity, the command line syntax of 'azbridge' uses 
elements that resemble SSH's equivalent tunnel functionality, especially the -L 
and -R arguments. The key difference to SSH is that azbridge always binds sockets
to an Azure Relay name, and that Azure Relay acts as the identifier for the 
tunnel and as network rendezvous point. In other words, you **always** need to 
pair an azbridge instance running as local forwarder with an azbridge running
as a remote forwarder on the other end.

SSH's dynamic SOCKS proxy functionality (SSH's -D option) is not supported since
it puts clients in control of selecting remote hosts after they've been bridged
into a foreign network, and this may pose significant security risks and might
inadvertently enable undesired access to resources on that foreign network.

`(C) -- 127.3.2.1:5000 -> (L) -- ['myname'] -> (R) -- 10.1.2.3:5000 -> (S)`

* **(C)** Client 
* **(L)** Local forwarder: `azbridge -L 127.3.2.1:5000:myname`
* **(R)** Remote forwarder: `azbridge -R myname:10.1.2.3:5000`
* **(S)** Server listening at `10.1.2.3:5000`


A single instance of azbridge can support multiple concurrent "local" listeners
and multiple "remote" forwarders concurrently, also in a mixed configuration.

The required Azure Relay connection string can either be supplied on the command
line, can be picked from an environment variable, or from a configuration file.

The connection string's embedded authentication/authorization information must
confer sufficient permissions for the desired operation(s) to be executed, e.g. 
for the "local" mode, the connection string must enable the bridge to send to 
the configured relay entity.

Arguments:

**-b bind_address**

Use bind_address on the local machine as the source address of
forwarding connections.  Only useful on systems with more than one
address.

**-D**   

Reserved. Not presently supported

**-E endpoint_uri**

Azure Relay endpoint URI (see -x).

**-F configfile**

Specifies an alternative per-user configuration file.  If a configuration 
file is given on the command line, the system-wide configuration file 
(Linux: /etc/azbridge/azbridge_config.machine.yml, 
Windows: %ALLUSERSPROFILE%\Microsoft\AzureBridge\azbridge_config.machine.yml) will be 
ignored. 
   
The default for the per-user configuration file is ~/.azurebridge/config
on Linux and %USERPROFILE%\.azurebridge\config on Windows.
   
**-g**    

Allows remote hosts to connect to local forwarded ports.
 
**-K policy_name**

Azure Relay shared access policy name to use (see -x).

**-k policy_key**

Azure Relay shared access policy key to use (see -x).

**-L [bind_address:]port[/port_name]{;...}:relay_name**<br/>
**-L local_socket[/port_name]{;...}:relay_name**<br/>

Specifies that connections to the given TCP/UDP port(s) or Unix socket(s)
on the local (client) host are to be bound (forwarded) to the given 
Azure Relay name. 

- `bind_address`: Optional local IP address to bind the 
   listener to. This may be a DNS name or a numerical IPv4 or
   IPv6 address expression and must resolve to a network endpoint
   on the local machine. When omitted, the listener is bound to
   all addresses ("any").
- `port`: TCP or UDP port number. TCP ports are the default. 
   UDP port numbers must be suffixed with `U`, 
   e.g. `-L 3434U:relay`.
- `local_socket`: Unix socket name. The expression will be 
   interpreted as a Unix socket name if it's not a valid `port`
   expression (i.e. not a number, with optional protocol suffix).
- `port_name`: Optional logical name for the port. If a "local"
   TCP port ought to be mapped to a different "remote" TCP port,
   a logical name allows this clearly, e.g. `-L 13389/rdp:relay`
   matches to `-R relay:rdp/3389` on the logical port name `rdp`, 
   which is bound to TCP port 13389 on the local side and TCP
   port 3389 on the remote side. For TCP/UDP, the default value 
   for `port_name` is the `port` value itself, meaning 
   `-L 13389:relay` can also be matched with `-R relay:13389/3389`. 
   For Unix sockets, this logical mapping can also be used, and 
   the default value is the name of `local_socket`. It is 
   permitted for multiple local TCP ports and Unix sockets or 
   for multiple UDP ports to use the same logical port name. UDP
   ports must always be matched to remote UDP forwarders.  
- `relay_name`: Name of the relay to bind the port(s) to.

There can be multiple local binding expressions given for a 
`relay_name`, separated by semicolons. The expressions can 
also mix protocols, e.g. `-L 7777;7777U:relay` binds the 
TCP and UDP ports 7777 to one relay name.

The bridge opens a listener on a TCP or UDP port or a Unix socket 
"here". The TCP or UDP listener is optionally bound to the 
specified `bind_address`. Whenever a connection
is made to the local port or socket, the connection is forwarded
to a connected remote bridge via the chosen Relay entity.

Port forwardings can also be specified in the configuration file.
Only the superuser can forward privileged ports.  IPv6 addresses
can be specified by enclosing the address in square brackets.

By default, the local port is bound in accordance with the
*GatewayPorts* configuration setting.  However, an explicit bind_address
may be used to bind the connection to a specific address.  The
bind_address of ``localhost'' indicates that the listening port
be bound for local use only, while an empty address or '*' indi-
cates that the port should be available from all interfaces.

The -L option can be used multiple times on a single command line,
but only once per `relay_name`. 

**-o option**

Can be used to give options in the format used in the configura-
tion file.  This is useful for specifying options for which there
is no separate command-line flag.

**-q**    

Quiet mode.  Causes most warning and diagnostic messages to be
suppressed.

**-R relay_name:[port_name/]hostport{;...}**<br/>
**-R relay_name:host:[port_name/]hostport{;...}**<br/>
**-R relay_name:[port_name/]local_socket{;...}**

Specifies that connections to the given Azure Relay name
and optional logical port name are to be forwarded to the 
given host and port, or Unix socket*. 
Whenever a connection is made to the Relay and logical port, 
the connection is forwarded to this listener (or a concurrently 
connected listener in a random load distribution fashion), and a 
then a forwarding connection is made to either port, host:hostport,
or local_socket, from the local machine.

- `relay_name`: Name of the relay to bind the forwarder to.
- `port_name`: Optional logical name for the port as defined by
   the local forwarder bound to this relay (see -L).   
- `host`: Host name or IP address to forward to.
- `port`: TCP or UDP port number. TCP ports are the default. 
   UDP port numbers must be suffixed with `U`, 
   e.g. `-R relay:3434U`. UDP forwarders can only be bound to 
   logcial UDP ports.
- `local_socket`: Unix socket name. The expression will be 
   interpreted as a Unix socket name if it's not a valid `port`
   expression (i.e. not a number, with optional protocol suffix).

There can be multiple local binding expressions given for a 
`relay_name`, separated by semicolons. The expressions can 
also mix protocols, e.g. `-R relay:7777;7777U` binds to 
TCP and UDP port forwarders for 7777 on one relay name.

Port forwardings can also be specified in the configuration file.
Privileged ports can be forwarded only when running with elevated privileges.  
IPv6 addresses can be specified by enclosing the address in square
brackets.

The -R option can be used multiple times on a single command line,
but only once for each `relay_name`.

**-S signature**

Azure Relay shared access signature (previously issued access token)
to use (see -x)

**-V**

Display the version number and exit.

**-v** 

Verbose mode.  Causes ssh to print debugging messages about its
progress. This is helpful in debugging connection, authentica-
tion, and configuration problems. Multiple -v options increase
the verbosity.  The maximum is 3.

**-x connection_string**

Connection String. Azure Relay connection string for the namespace
or for a specific Azure Relay. The Connection String properties
can be overridden by the -E (Endpoint), -K (SharedAccessKeyName),
-k (SharedAccessKey), -S (SharedAccessSignature) arguments.

If an EntityPath is specified in the connection string, that name 
is the only valid option for the relay_name expressions in the
-L and -R options or for expressions in the effective configuration
file.

The connection string can be set via the AZURE_BRIDGE_CONNECTIONSTRING
environment variable. 
 
## Configuration File

The configuration file is a YAML file that specifies options that apply 
to the machine or user. The machine level options are always read and 
then complemented by or overridden by the user-level options.

The configuration file can exist in three locations:

1. Machine configuration, always loaded if present.
   Linux: /etc/azurebridge/azurebridge_config
   Windows: %ALLUSERSPROFILE%\Microsoft\AzureBridge\azbridge_config
2. User configuration, overrides and complements machine config.
   Linux: ~/.azurebridge/config
   Windows: %USERPROFILE%\.azurebridge\config
3. Override user configuration location for current execution with 
   the -f option.

The configuration file holds a range of configuration options that, just
like the command line options, partially lean on similar expressions
used in SSH, even though the configuration format differs from that of SSH.

The "LocalForward" and "RemoteForward" sections define bindings as with
the -L and -R command line options above.

* **AddressFamily** - Specifies which address family to use when connecting.  Valid
  arguments are "any", "inet" (use IPv4 only), or "inet6"
  (use IPv6 only).  The default is "any".
* **AzureRelayConnectionString** - Azure Relay connection string for a Relay 
  namespace. Only one namespace connection string can be specified per configuration
  file.
* **AzureRelayEndpoint** - Azure Relay endpoint URI for a Relay namespace. Overrides 
  the 'Endpoint' property of the connection string, if present.
* **AzureRelaySharedAccessKeyName** - Azure Relay shared access policy name. Overrides 
  the 'SharedAccessKeyName' property of the connection string, if present.
* **AzureRelaySharedAccessKey** - Azure Relay shared access policy key. Overrides 
  the 'SharedAccessKey' property of the connection string, if present.
* **AzureRelaySharedAccessSignature** - Azure Relay shared access policy signature. Overrides 
  the 'SharedAccessSignature' property of the connection string, if present.
* **BindAddress** - Use the specified address on the local machine as the source
  address of the connection.  Only useful on systems with more than
  one address.  
* **ClearAllForwardings** - Specifies that all local, and remote port forwardings
  specified in the configuration files or on the command line be
  cleared.  This option is primarily useful when used from the
  command line to clear port forwardings set in configura-
  tion files. The argument must be "true" or "false".  The default is "false".
* **ConnectionAttempts** - Specifies the number of tries (one per second) to make
  before exiting.  The argument must be an integer.  This may be useful in scripts
  if the connection sometimes fails.  The default is 1.
* **ConnectTimeout** - Specifies the timeout (in seconds) used when connecting to the
  relay server, instead of using the default system TCP timeout.
  This value is used only when the target is down or really
  unreachable, not when it refuses the connection.
* **ExitOnForwardFailure** - Specifies whether azbridge(1) should terminate the 
  connection if it  cannot set up all requested local, and remote port forwardings, 
  (e.g. if either end is unable to bind and listen on a specified port). 
  The argument must be "true" or "false". The default is "false".
* **GatewayPorts** - Specifies whether remote hosts are allowed to connect to local
  forwarded ports.  By default, azbridge(1) binds local port forwardings
  to the loopback address.  This prevents other remote hosts from
  connecting to forwarded ports.  GatewayPorts can be used to specify that azbridge 
  should bind local port forwardings to the wildcard address, thus allowing remote 
  hosts to connect to forwarded ports. The argument must be "true" or "false".  
  The default is "false". 
* **LocalForward** - Specifies that a (set of) TCP ports on the local machine 
  shall be forwarded via the Azure Relay. Each entry can have four properties,
  "BindAddress", "Port", "LocalSocket", and "RelayName". See [below](#localforward-properties) for 
  details.
* **LogLevel** - Gives the verbosity level that is used when logging messages 
  from azbridge(1). The possible values are: QUIET, FATAL, ERROR, INFO, VERBOSE, 
  DEBUG, DEBUG1, DEBUG2, and DEBUG3.  The default is INFO.
  DEBUG and DEBUG1 are equivalent.  DEBUG2 and DEBUG3 each specify
  higher levels of verbose output.
* **RemoteForward** - Specifies that a TCP port on the remote machine be bound to 
  a name on the Azure Relay. Each entry can have four properties, "RelayName", "Host", 
  "HostPort", and "LocalSocket". See [below](#remoteforward-properties) for details.

### LocalForward properties

The following properties are defined for LocalForward. LocalForward is a list
and multiple entries are permitted.

* **RelayName** - name of the Azure Relay name to bind to
* **ConnectionString** - optional Azure Relay connection string to use just for this forwarder, overriding the global **AzureRelayConnectionString** property.

For a single port binding on the Relay name, the following properties can be 
used on the same entry. For multiple bindings they can be used to form a list.

* **BindAddress** - network address to bind the socket to
* **PortName** - Logical port name
* **BindPort** - TCP port to bind the socket to
* **BindLocalSocket** - named UNIX socket to bind to
* **RemoteHostName** - optionally, remote host name represented by this entry (purely informational)

Examples:

- Single listener binding:
  ``` YAML
     - RelayName: myrelay
       BindAddress: 127.0.8.1
       BindPort: 8888    
  ```
- Multiple listener binding:
  ``` YAML
     - RelayName: myrelay
       Bindings:          
        - BindAddress: 127.0.8.1
          BindPort: 5671
          PortName: amqps 
        - BindAddress: 127.0.8.1
          BindPort: 5672    
          PortName: amqp
  ```


Using `BindAddress` and `BindPort` is mutually exclusive with use of the 
`BindLocalSocket` option. The bind_address argument is optional and when
omitted, the default is for the listener to bind to all interfaces.

The `RelayName` option is always required.

The `RemoteHostName` is property optional and used for documentation. Host
names that shall resolve to the -L local forwarder address need to
be added to the local hosts file.

The `ConnectionString` property is optional and overrides the global settings
if supplied.

### RemoteForward properties

The following properties are defined for RemoteForward. RemoteForward is a list
and multiple entries are permitted.

* **RelayName** - name of the Azure Relay name to bind to
* **ConnectionString** - Azure Relay connection string to use for this forwarder

For a single port binding on the Relay name, the following properties can be 
used on the same entry. For multiple bindings they can be used to form a list.

* **Host** - network address to forward to
* **HostPort** - TCP port on the host to forward to
* **PortName** - Logical port name
* **LocalSocket** - named UNIX socket forward to

Examples:

- Single listener binding:
  ``` YAML
     - RelayName: myrelay
       Host: localhost
       HostPort: 8888    
  ```
- Multiple listener binding:
  ``` YAML
     - RelayName: myrelay
       Bindings:
        - Host: broker.corp.example.com
          HostPort: 5671
          PortName: amqps 
        - Host: broker.corp.example.com
          HostPort: 5672    
          PortName: amqp
  ```

Using `Host` and `HostPort` is mutually exclusive with use of the 
`LocalSocket` option. The host argument is optional and when
omitted, the default is for the forwarder to connect to the local machine.

The `RelayName` option is always required.

The `ConnectionString` property is optional and overrides the global settings
if supplied.

## Configuration examples

### Example 1

This example shows a local configuration that enables local forwarders for 
three remote computers on different intranets via RDP.

``` yaml
---
GatewayPorts : no
LocalForward:

  # RDP to remote machine abcxyz
  - BindAddress: 127.0.10.1
    HostName: abcxyz.intra-de.example.com
    BindPort: 3389
    RelayName: abcxyzrdp

# SQL to remote machine abcxyz
  - BindAddress: 127.0.10.1
    HostName: abcxyz.intra-de.example.com
    BindPort: 1433
    RelayName: abcxyzsql

  # RDP to remote machine defijk
  - BindAddress: 127.0.10.2
    HostName: defijk.intra-us.example.com
    BindPort: 3389
    RelayName: defijkrdp
  
  # RDP to remote machine ghiuvw
  - BindAddress: 127.0.10.3
    HostName: ghiuvw.intra-jp.example.com
    BindPort: 3389
    RelayName: ghiuvwrdp 
```

## Example 2

This example shows a local configuration that enables the remote 
forwarder for RDP on the computer `abcxyz` from the prior example.


``` yaml
---
GatewayPorts : no
RemoteForward:

  # RDP to this machine abcxyz
  - RelayName: abcxyzrdp
    HostPort: 3389
```

## Example 3

This example shows a local configuration that enables the remote 
forwarder for RDP to a computer `defijk` from the prior example
on the remote network, but from a different computer.


``` yaml
---
GatewayPorts : no
RemoteForward:
# RDP to remote machine defijk
  - RelayName: defijkrdp
    Host: defijk.intra-us.example.com
    HostPort: 3389
```

## Example 4

This example shows a configuration that allows access
to a remote SQL server on a local network address of this computer.

``` yaml
---
GatewayPorts : true
LocalForward:

  # SQL to remote machine abcxyz
  - BindAddress: 10.10.100.2
    HostName: abcxyz.intra-de.example.com
    BindPort: 1433
    RelayName: abcxyzsql
```


