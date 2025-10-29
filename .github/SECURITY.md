# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in this project, please report it immediately by emailing the security team at **rutova2@gmail.com**.

Please include the following information in your report:

- Description of the vulnerability
- Steps to reproduce or proof of concept
- Impact assessment (what can be exploited)
- Any suggested fixes or mitigations
- Your contact information

We will acknowledge receipt of your report within **48 hours** and provide a more detailed response within **72 hours** with next steps and estimated timeline for a fix.

## Supported Versions

| Version | Support Level |
|---------|-------------|
| v2.0.x | **Full support** - Security patches and bug fixes |
| v1.x   | **Security fixes only** - Critical security patches only |

## Security Best Practices

When using this actor framework in production:

1. **Input Validation**: Always validate message content and parameters
2. **Timeout Configuration**: Set appropriate timeouts for all external calls
3. **Error Handling**: Implement proper exception handling in actors
4. **Logging**: Avoid logging sensitive information (credentials, tokens, PII)
5. **Rate Limiting**: Use middleware to prevent message flooding
6. **Authentication**: Validate sender credentials when needed
7. **Network Security**: Secure remote actor communication channels

## Reporting Security Issues

- **Private Reporting**: Email rutova2@gmail.com (preferred)
- **Public Reporting**: GitHub Security Advisory or GitHub Issues (for non-critical issues)
- **Response Time**: We aim to respond to all security reports within 3 business days

## Disclosure Process

1. Security report received via email
2. Team acknowledges receipt within 48 hours
3. Team investigates and validates the report
4. Patch developed and tested
5. Coordinated disclosure with reporter
6. Public advisory published
7. Version bump and release

## Security Contact

For all security-related matters, please contact:

**Email**: rutova2@gmail.com

**PGP Key**: Available upon request

## Security Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CWE Top 25](https://cwe.mitre.org/top25/)
- [.NET Security Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/security/)

## Acknowledgements

We appreciate the efforts of security researchers and the community in helping keep this project secure. All responsible disclosures will be properly credited unless requested otherwise.
