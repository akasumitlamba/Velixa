package com.velixa.app;
import java.math.BigInteger;
import java.security.*;
import java.io.ByteArrayOutputStream;
final class SrpClient {
 // RFC 5054 2048-bit group; SHA-256 and padded evidence match the Windows SRP implementation.
 static final BigInteger N=new BigInteger("AC6BDB41324A9A9BF166DE5E1389582FAF72B6651987EE07FC3192943DB56050A37329CBB4A099ED8193E0757767A13DD52312AB4B03310DCD7F48A9DA04FD50E8083969EDB767B0CF6095179A163AB3661A05FBD5FAAAE82918A9962F0B93B855F97993EC975EEAA80D740ADBF4FF747359D041D5C33EA71D281E446B14773BCA97B43A23FB801676BD207A436C6481F1D2B9078717461A5B9D32E688F87748544523B524B0D57D5EA77A2775D2ECFA032CFBDBF52FB3786160279004E57AE6AF874E7303CE53299CCC041C7BC308D82A5698F3A8D0C38271AE35F8E9DBFBB694B5C803D89F7AE435DE236D525F54759B65E372FCD68EF20FA7111F9E4AFF73",16), G=BigInteger.valueOf(2);
 final BigInteger a=new BigInteger(256,new SecureRandom()).setBit(255), A=G.modPow(a,N);BigInteger B,S,M;
 static byte[] digest(byte[]... values)throws Exception{MessageDigest h=MessageDigest.getInstance("SHA-256");for(byte[] v:values)h.update(v);return h.digest();}
 static byte[] pad(BigInteger n){byte[] b=n.toByteArray(),p=new byte[256];int count=Math.min(b.length,p.length);System.arraycopy(b,b.length-count,p,p.length-count,count);return p;}
 static BigInteger hash(BigInteger... values)throws Exception{ByteArrayOutputStream b=new ByteArrayOutputStream();for(BigInteger v:values)b.write(pad(v));return new BigInteger(1,digest(b.toByteArray()));}
 String proof(byte[] salt,String identity,String password,String server)throws Exception{if(server.length()>1024)throw new SecurityException();B=new BigInteger(server,16).mod(N);if(B.signum()==0)throw new SecurityException();BigInteger x=new BigInteger(1,digest(salt,digest((identity+":"+password).getBytes("UTF-8"))));BigInteger u=hash(A,B);if(u.signum()==0)throw new SecurityException();S=B.subtract(hash(N,G).multiply(G.modPow(x,N))).mod(N).modPow(a.add(u.multiply(x)),N);M=hash(A,B,S);return M.toString(16);}
 String finish(String evidence)throws Exception{if(evidence.length()>1024||!hash(A,M,S).equals(new BigInteger(evidence,16)))throw new SecurityException("Pairing code does not match");return hash(S).toString(16);}
}
