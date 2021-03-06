/* Code Pulse: a real-time code coverage tool, for more information, see <http://code-pulse.com/>
 *
 * Copyright (C) 2014-2017 Code Dx, Inc. <https://codedx.com/>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package com.secdec.bytefrog.agent.message.test

import java.io.ByteArrayInputStream
import java.io.ByteArrayOutputStream
import java.io.InputStream
import java.io.OutputStream
import java.net.Socket
import java.net.SocketImpl

import org.scalatest.FunSpec
import org.scalatest.concurrent.AsyncAssertions
import org.scalatest._
import org.scalatest.Matchers._
import org.scalamock.scalatest.MockFactory

import com.codedx.codepulse.agent.init.DataConnectionHandshake
import com.codedx.codepulse.agent.message.MessageSenderManager
import com.secdec.bytefrog.agent.util.ControlSimulation
import com.secdec.bytefrog.agent.util.MockHelpers
import com.codedx.codepulse.agent.util.SocketFactory

class SenderManagerV1Spec extends FunSpec with Matchers with MockFactory with AsyncAssertions with ControlSimulation with MockHelpers {

	class UselessSocket extends Socket {
		val in = new ByteArrayInputStream(Array())
		val out = new ByteArrayOutputStream

		override def getInputStream = in
		override def getOutputStream = out
	}

	class MockableSocketFactory extends SocketFactory("localhost", 8765)

	class PretendSocket(in: InputStream, out: OutputStream) extends Socket {
		override def getInputStream = in
		override def getOutputStream = out
	}

	describe("MessageSenderManager.start") {
		it("should fail if the socket factory fails to connect to a socket") {
			val sf = mock[MockableSocketFactory]
			(sf.connect _).expects().anyNumberOfTimes.returning(null)

			val m = new MessageSenderManager(sf, mock[DataConnectionHandshake], mock[BufferPoolMockable], 3, 1)

			try {
				m.start should equal(false)
			} finally {
				m.shutdown
			}

		}

		it("should fail if the handshake fails") {
			val sf = mock[MockableSocketFactory]
			(sf.connect _).expects().anyNumberOfTimes.returning { new UselessSocket }

			val handshaker = mock[DataConnectionHandshake]
			(handshaker.performHandshake _).expects(*, *).anyNumberOfTimes.returning(false)

			val m = new MessageSenderManager(sf, handshaker, mock[BufferPoolMockable], 3, 1)

			try {
				m.start should equal(false)
			} finally {
				m.shutdown
			}
		}

		it("should succeed when the socket handshake works") {
			val sf = mock[MockableSocketFactory]
			(sf.connect _).expects().anyNumberOfTimes.returning { new UselessSocket }

			val handshaker = mock[DataConnectionHandshake]
			(handshaker.performHandshake _).expects(*, *).anyNumberOfTimes.returning(true)

			val m = new MessageSenderManager(sf, handshaker, mock[BufferPoolMockable], 3, 1)

			try {
				m.start should equal(true)
			} finally {
				m.shutdown
			}
		}

		it("should return false when called after the first time") {
			val sf = mock[MockableSocketFactory]
			(sf.connect _).expects().anyNumberOfTimes.returning { new UselessSocket }

			val handshaker = mock[DataConnectionHandshake]
			(handshaker.performHandshake _).expects(*, *).anyNumberOfTimes.returning(true)

			val m = new MessageSenderManager(sf, handshaker, mock[BufferPoolMockable], 3, 1)

			try {
				m.start should equal(true)
				m.start should equal(false)
			} finally {
				m.shutdown
			}
		}
	}
}