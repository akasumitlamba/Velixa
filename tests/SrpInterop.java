package com.velixa.app;
import java.io.*;
import java.util.Base64;
public final class SrpInterop {
 public static void main(String[] args)throws Exception{BufferedReader r=new BufferedReader(new InputStreamReader(System.in));String identity=r.readLine(),password=r.readLine(),salt=r.readLine(),server=r.readLine();SrpClient client=new SrpClient();String proof=client.proof(Base64.getDecoder().decode(salt),identity,password,server);System.out.println(client.A.toString(16));System.out.println(proof);System.out.flush();System.out.println(client.finish(r.readLine()));System.out.flush();}
}
